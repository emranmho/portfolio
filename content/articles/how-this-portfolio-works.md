---
title: "How this portfolio works"
summary: "Architecture walkthrough: why SQLite over Postgres, why no Redis, why MediatR on only two endpoints, and what changes at 100× scale."
date: 2026-07-08
tags: [architecture, dotnet, trade-offs]
---

# How this portfolio works

Most portfolio sites are static pages that *claim* things: "experienced with CI/CD,
observability, clean architecture." This one is built so the claims are checkable. The site
you're reading is a thin Next.js client of a real, production-deployed .NET 10 API. The
metrics on the [status page](/status) are measured by middleware in that API. The
[playground](/playground) lets you call every endpoint yourself, including tripping the rate
limiter on purpose. This article walks through the whole system and — more importantly —
the things it deliberately does *not* use.

## The system in one diagram

```
                    ┌──────────────────────┐
                    │  Visitors / curl      │
                    └──────────┬───────────┘
                               │ HTTPS
                    ┌──────────▼───────────┐
                    │  Traefik (Dokploy)    │  auto-TLS, reverse proxy
                    │  emran.blog     → web │
                    │  api.emran.blog → api │
                    └─────┬───────────┬────┘
                          │           │
           ┌──────────────▼──┐   ┌────▼──────────────────┐
           │ web (Next.js)   │──▶│ api (.NET 10)         │
           │ Server comps    │   │ Minimal API · CQRS×2  │
           │ ISR (60s)       │   │ Rate limiter · JWT    │
           └─────────────────┘   └────┬─────────┬────────┘
                                      │         │
                               ┌──────▼──┐ ┌────▼─────────┐
                               │ SQLite  │ │ content/*.md │
                               │ (volume)│ │ (in repo)    │
                               └─────────┘ └──────────────┘
```

Two containers on one VPS. Traefik terminates TLS and routes by domain. The frontend
server-renders from the API with 60-second ISR, so crawlers get real HTML and you never see
a loading spinner for content. That's the entire production topology.

## Content is a git repository, not a CMS

Articles are markdown files with YAML frontmatter, living in the same repo as the code.
Projects are JSON files. At startup the API parses them with Markdig, renders HTML
server-side, and upserts everything into SQLite.

Publishing this article was: write markdown, `git push`. CI runs the tests, the deploy
rebuilds the image with the content baked in, and the new container comes up already
serving it. There is no admin panel, no write API, no auth for content — because there's
nothing to protect. The version history of every article is `git log`.

Baking content into the image (instead of mounting a folder) was deliberate: deployments
are immutable. A deployed image either has the article or it doesn't; no drift between
what's in git and what's on disk.

## Why SQLite, not Postgres

The only database workload here is metrics aggregates and ingested content. That's one
writer (the API), a handful of reads per page view, and a few kilobytes per minute of
writes. SQLite handles orders of magnitude more than that from a single file on a Docker
volume.

Choosing Postgres would have added: a second stateful container, connection strings and
credentials, backup orchestration, and version upgrades — all to serve traffic SQLite
doesn't notice. The backup story instead is a nightly cron copying one file off-box.

**When it flips:** multiple app instances writing concurrently, or data that must survive
independent of the app host. At that point: managed Postgres, not self-hosted — the
operational work is the part worth paying to remove.

## Why there is no Redis

Rate limiting and caching both live in-process, using ASP.NET Core's built-in
`RateLimiter` middleware and `IMemoryCache`. Redis exists to share state *between
instances*. There is one instance. A Redis container here would be cargo cult — a tool
chosen for the resume, not the system.

**When it flips:** the moment there are two API replicas behind the proxy, in-memory rate
limit counters mean each replica grants its own quota. That's the day Redis (or another
shared store) earns its place.

## Why MediatR on exactly two endpoints

The API has around ten endpoints. Two of them — `GET /api/articles/{slug}` and
`GET /api/metrics` — go through MediatR with a proper `Query`/`Handler`/`Response` triple.
The other eight are plain minimal-API handlers calling application services directly.

This is the part reviewers ask about most, so to be explicit: CQRS-via-mediator is a
pattern I use professionally, and the two endpoints demonstrate its shape — thin HTTP
layer, testable handler, request/response contracts. Applying it to all ten endpoints
would add three files of ceremony per endpoint in a codebase where the "bus" has exactly
one subscriber per message. Knowing where a pattern stops paying rent is the actual skill
being demonstrated.

**When it flips:** more endpoints, cross-cutting behaviors (validation, logging,
transactions) as pipeline stages, or a team that benefits from the uniformity.

## Why the observability is hand-rolled

The status page could have been Prometheus + Grafana. Instead, a custom middleware records
route, status code, and elapsed milliseconds for every request; a background service
flushes per-minute aggregates (count, errors, p50/p95/p99) to SQLite every 30 seconds; and
`GET /api/metrics` serves the summary that both the [status page](/status) and the home
page render.

Two reasons. First, proportionality again: Prometheus + Grafana is two more containers,
scrape configs, and dashboard JSON to serve one site's worth of metrics. Second — and this
is the honest one — for a portfolio, *building* the metrics pipeline demonstrates more
than *configuring* one. Percentile estimation, bucketing, healthcheck exclusion (the
Docker healthcheck hits `/api/health` every few seconds and shouldn't count as traffic)
are all decisions you can read in the source.

**When it flips:** immediately, at work. This is the one choice I'd reverse first in a
professional system — OpenTelemetry → Prometheus → Grafana with alerting is the correct
answer the moment anyone is on call.

## The deploy pipeline

```
git push → GitHub Actions (dotnet test / lint+build) → Dokploy webhook
         → image build (GIT_SHA baked in) → health check → traffic
         → CI verifies /api/health returns the pushed SHA
```

Dokploy (a self-hosted PaaS) owns the plumbing: builds, TLS, restarts, volumes. The
engineering I kept for myself is the part a PaaS can't do: tests gate every deploy, and
after deploying, CI curls the health endpoint until the reported git SHA matches the
commit that triggered the build. A deploy that doesn't verify isn't green. The health
check also means a container that fails startup never receives traffic — the previous one
keeps serving.

Rollback is redeploying the last good SHA. Deploy history — which you can see on the
[status page](/status) — comes from the API recording its own `GIT_SHA` at startup.

## What changes at 100× scale

| Component | Now | At 100× |
|---|---|---|
| Database | SQLite on a volume | Managed Postgres |
| Cache / rate limit | In-process | Redis, shared across replicas |
| Observability | Custom middleware → SQLite | OTel → Prometheus → Grafana, alerting |
| CQRS | 2 endpoints | Uniform, with pipeline behaviors |
| Auth | Demo JWT toy | Real IdP the moment a private resource exists |
| Deploys | Dokploy on one VPS | Registry + IaC + orchestration when the team needs it |

None of these upgrades are speculative — each has a concrete trigger listed above. The
system is small *on purpose*, and every omission is a decision with a documented flip
point, not a gap.

The [source is on GitHub](https://github.com/emranmho). Clone it, run `dotnet test`, and
check whether this article told you the truth.
