# emran.blog — Portfolio v2

> A portfolio where the backend **is** the portfolio. The site is a thin client of a real, observable, production-deployed .NET API — every metric on the status page is real, every endpoint is publicly explorable, and the architecture is documented honestly, including what was deliberately *not* built.

**Live:** https://emran.blog · **API:** https://api.emran.blog/docs

[![api](https://github.com/emranmho/portfolio/actions/workflows/api.yml/badge.svg)](https://github.com/emranmho/portfolio/actions/workflows/api.yml) [![web](https://github.com/emranmho/portfolio/actions/workflows/web.yml/badge.svg)](https://github.com/emranmho/portfolio/actions/workflows/web.yml)

---

## Table of contents

1. [Philosophy](#philosophy)
2. [Architecture overview](#architecture-overview)
3. [Tech stack](#tech-stack)
4. [Monorepo structure](#monorepo-structure)
5. [Phase 1 — Backend (.NET 10 API)](#phase-1--backend-net-10-api)
6. [Phase 2 — Infra + CI/CD](#phase-2--infra--cicd)
7. [Phase 3 — Frontend rebuild (Next.js)](#phase-3--frontend-rebuild-nextjs)
8. [Phase 4 — Differentiators](#phase-4--differentiators)
9. [Phase 5 — Content + polish](#phase-5--content--polish)
10. [Design decisions & trade-offs](#design-decisions--trade-offs)
11. [Local development](#local-development)
12. [Roadmap checklist](#roadmap-checklist)
13. [Idea backlog (post-launch only)](#idea-backlog-post-launch-only)

---

## Philosophy

Three rules drive every decision in this repo:

1. **Prove, don't claim.** The resume says "CI/CD, observability, clean architecture." This repo demonstrates each claim with running code instead of bullet points.
2. **Proportionate architecture.** Patterns are used where they teach something, not everywhere. CQRS on two endpoints as a demonstration; plain handlers elsewhere. SQLite instead of Postgres. No Redis. Every omission is documented in [trade-offs](#design-decisions--trade-offs) — knowing when *not* to use a tool is the senior signal.
3. **Never half-built.** Each phase ends deployed and working. A dead Grafana dashboard is worse than no dashboard.

---

## Architecture overview

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
               │ ISR (60s)       │   │ Rate limiter · JWT demo│
               └─────────────────┘   └────┬─────────┬────────┘
                                          │         │
                                   ┌──────▼──┐ ┌────▼─────────┐
                                   │ SQLite  │ │ content/*.md │
                                   │ (volume)│ │ (in repo)    │
                                   └─────────┘ └──────────────┘

  git push ─▶ GitHub Actions (tests gate) ─▶ Dokploy API (build + deploy on VPS)
                                                 │
                                                 └─ health check → no traffic to a broken container
```

- **2 app containers** on one VPS (`web`, `api`), managed by **Dokploy**, which supplies the Traefik reverse proxy, TLS, builds, and deploys. That's the whole system.
- Content (articles, project data) lives **in git as markdown/JSON**, ingested by the API at startup. Publishing = `git push`. No CMS, no admin panel, no auth for writes.
- Metrics are collected by custom middleware into SQLite and exposed via the API's own `/api/metrics` — the status page shows **real** data.

---

## Tech stack

| Layer | Choice | Why |
|---|---|---|
| Backend | **.NET 10** (LTS, supported to Nov 2028), C#, Minimal APIs | Primary professional stack; LTS over STS for a long-lived personal site |
| ORM / DB | **EF Core 10 + SQLite** | Zero-ops, file-backed, more than enough for portfolio traffic |
| CQRS (demo) | **MediatR** | Used on exactly 2 endpoints as a pattern demonstration |
| Markdown | **Markdig** (+ YAML frontmatter) | Articles as version-controlled `.md` files |
| Logging | **Serilog** (console sink, structured JSON) | Matches production experience; grep-able via `docker logs` |
| API docs | Built-in **OpenAPI** (`Microsoft.AspNetCore.OpenApi`) + **Scalar** UI | Modern replacement for Swagger UI |
| Auth (demo only) | **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) | Playground demo of token issuance/validation — not real security |
| Rate limiting | Built-in `Microsoft.AspNetCore.RateLimiting` | Protects the public playground; no Redis needed on a single instance |
| Testing | **xUnit + WebApplicationFactory** | Integration tests over the real HTTP pipeline with an in-memory SQLite DB |
| Frontend | **Next.js (latest, App Router) + TypeScript** | Server components + ISR; scaffold with current `create-next-app`, don't pin |
| Styling | **Tailwind CSS v4** | Utility-first; design tokens as CSS variables |
| UI components | **Hand-rolled (~10 components)** — no shadcn/ui | Avoids the template look; a portfolio needs 5% of a component library |
| Fonts | **JetBrains Mono** (headings/data/labels) + **Inter** (body) via `next/font` | The mono is the personality |
| Code highlighting | **Shiki** | Server-side highlighting for articles, zero client JS |
| Deployment platform | **Dokploy** (self-hosted PaaS on the VPS) | Push-to-deploy, builds, env/volume management, health checks — already in use |
| Reverse proxy | **Traefik** (bundled with Dokploy) | Automatic Let's Encrypt TLS, domain-based routing; configured via Dokploy UI |
| Containers | **Docker** (Dockerfiles per app, built by Dokploy) | Single-VPS deployment target |
| CI/CD | **GitHub Actions (tests gate) → Dokploy (build + deploy)** | CI proves quality, Dokploy does the plumbing; deploys only trigger after tests pass |
| Hosting | **VPS** (existing, running Dokploy) | Full control, one box |

---

## Monorepo structure

```
portfolio/
├── apps/
│   ├── api/                          # .NET 10 backend
│   │   ├── src/
│   │   │   ├── Portfolio.Api/            # HTTP layer: endpoints, middleware, Program.cs
│   │   │   ├── Portfolio.Application/    # use cases: CQRS handlers, DTOs, interfaces
│   │   │   ├── Portfolio.Domain/         # entities + domain logic, zero dependencies
│   │   │   └── Portfolio.Infrastructure/ # EF Core, content ingestion, implementations
│   │   ├── tests/
│   │   │   └── Portfolio.Tests/          # unit + integration (WebApplicationFactory)
│   │   ├── Dockerfile
│   │   └── Portfolio.sln
│   └── web/                          # Next.js frontend
│       ├── src/
│       │   ├── app/                      # App Router pages
│       │   ├── components/               # the ~10 hand-rolled components
│       │   └── lib/                      # API client, helpers
│       ├── Dockerfile
│       └── package.json
├── content/                          # publishing = git push
│   ├── articles/                         # *.md with YAML frontmatter
│   ├── projects/                         # *.json project definitions
│   └── whoami.json                       # the /api/whoami payload
├── infra/
│   ├── docker-compose.local.yml          # local prod-shaped run only (Dokploy owns prod)
│   └── dokploy.md                        # app settings, domains, volumes, env vars documented
├── .github/
│   └── workflows/
│       ├── api.yml                       # path-filtered: apps/api/** , content/**
│       └── web.yml                       # path-filtered: apps/web/**
└── README.md                         # ← you are here
```

**Monorepo rules:** each workflow is path-filtered so a frontend commit doesn't rebuild the API. `content/` changes trigger an API redeploy (content is baked into the image at build for immutability — see Phase 2).

---

## Phase 1 — Backend (.NET 10 API)

**Goal:** a deployed-locally-runnable API that serves every byte the site needs, with real metrics. **Milestone:** `dotnet run`, hit every endpoint, `/docs` renders.

### 1.1 API contract

| Method | Route | Description | Notes |
|---|---|---|---|
| GET | `/api/health` | Liveness + version + git SHA | Used by Docker healthcheck & deploy gate |
| GET | `/api/whoami` | Identity JSON (the terminal-hero payload) | From `content/whoami.json` |
| GET | `/api/projects` | List projects | Supports `?stack=go\|dotnet` filter |
| GET | `/api/projects/{slug}` | Project detail | 404 with ProblemDetails if unknown |
| GET | `/api/articles` | Article list (metadata only) | Sorted by date desc |
| GET | `/api/articles/{slug}` | Full article (rendered HTML + raw md) | **CQRS/MediatR demo #1** |
| GET | `/api/metrics` | Real request counts, latency percentiles, uptime | **CQRS/MediatR demo #2** |
| POST | `/api/auth/token` | Issues a short-lived (15 min) demo JWT | No credentials required — it's a teaching toy |
| GET | `/api/secret` | JWT-protected; echoes your decoded claims | Returns 401 without a token — that's the demo |
| GET | `/docs` | Scalar API reference | Generated from built-in OpenAPI |

### 1.2 Layered structure (proportionate, not ceremonial)

- **Domain** — `Project`, `Article`, `MetricSample`, value objects like `Slug`. No packages, no attributes. Small on purpose.
- **Application** — interfaces (`IArticleReader`, `IMetricsStore`), DTOs, and the two MediatR features:
  - `Features/Articles/GetArticleBySlug/` → `Query`, `Handler`, `Response`
  - `Features/Metrics/GetMetricsSummary/` → `Query`, `Handler`, `Response`
  - All other endpoints are **plain minimal-API handlers** calling Application services directly. The README-level point: MediatR everywhere in a 10-endpoint API is ceremony; here it exists to demonstrate the pattern's shape.
- **Infrastructure** — `PortfolioDbContext` (EF Core/SQLite), `MarkdownContentIngester` (Markdig + frontmatter, runs at startup, upserts into SQLite), `SqliteMetricsStore`.
- **Api** — `Program.cs` (DI, pipeline), endpoint groups (`MapProjectEndpoints()` etc.), middleware.

Dependency rule: `Api → Application → Domain`; `Infrastructure → Application` (implements its interfaces). Enforced by project references — nothing else can compile.

### 1.3 Key packages

```
Portfolio.Api:            Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore,
                          Serilog.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer
Portfolio.Application:    MediatR
Portfolio.Infrastructure: Microsoft.EntityFrameworkCore.Sqlite, Markdig, YamlDotNet
Portfolio.Tests:          xunit, Microsoft.AspNetCore.Mvc.Testing, FluentAssertions
```

### 1.4 Metrics middleware (the honest-status-page engine)

Custom `MetricsMiddleware` registered early in the pipeline:

- Records per-request: route template, status code, elapsed ms, timestamp.
- Buffers in memory; a background `IHostedService` flushes aggregates to SQLite every 30s (per-minute buckets: count, error count, p50/p95/p99 via simple sorted-sample estimation).
- `/api/metrics` returns: uptime since process start + persisted deploy history, requests today, avg/p95 latency, error rate, per-endpoint breakdown.
- Excludes `/api/health` from stats so the Docker healthcheck doesn't inflate traffic.

This replaces Prometheus + Grafana deliberately — see [trade-offs](#design-decisions--trade-offs).

### 1.5 Demo JWT flow (auth as a feature, not security)

- `POST /api/auth/token` → issues an HS256 JWT, 15-minute expiry, claims: `sub: "guest-{random}"`, `role: "explorer"`, `iss/aud` set. Signing key from env (`JWT_DEMO_KEY`).
- `GET /api/secret` → `[RequireAuthorization]`; returns a playful payload plus your decoded claims so visitors *see* what a JWT carries.
- Playground UX (Phase 4): call without token → **401**, get token → call again → **200**. That contrast is the lesson.
- Explicitly labeled "demo" in `/docs`. There is nothing real to protect — content writes happen via git.

### 1.6 Rate limiting

Built-in `RateLimiter` middleware: fixed window, e.g. **20 req/min per IP** on `/api/*` (health excluded), with `Retry-After` + rate-limit headers exposed so the playground can display them. Single instance ⇒ in-memory is correct; Redis would be cargo cult here.

### 1.7 Error handling, logging, tests

- Global `ProblemDetails` (RFC 9457) for all errors; no stack traces in responses.
- Serilog structured JSON to stdout → `docker logs` is the log store at this scale.
- Tests: unit tests for handlers/ingestion parsing; integration tests via `WebApplicationFactory` + SQLite in-memory covering every endpoint's happy path, 404s, 401 on `/api/secret`, and the rate limiter's 429.

---

## Phase 2 — Infra + CI/CD (Dokploy)

**Goal:** push to `main` → tested → built and deployed by Dokploy → health-verified. **Milestone:** `curl https://api.emran.blog/api/whoami` returns real JSON over HTTPS with zero manual steps. Since Dokploy is already running on the VPS, this phase is an evening, not a weekend.

### 2.1 Dockerfiles (Dokploy builds these)

- **API** — multi-stage: `sdk:10.0` build/publish (`-c Release`), runtime `aspnet:10.0-alpine` (base image already ships a non-root `app` user — don't re-create it, `addgroup: group 'app' in use` build failure if you try), `HEALTHCHECK CMD wget -qO- http://localhost:8080/api/health || exit 1`. `content/` is **copied into the image** at build — content changes are immutable deployments, not files mutated on a server. Accepts a `GIT_SHA` build arg exposed via `/api/health` (left as the literal `dev` placeholder for now — see 2.3). Runtime env also needs `DOTNET_EnableWriteXorExecute=0`: without it the container exits instantly with code `139` (SIGSEGV, zero logs) on this VPS's kernel/hypervisor, which doesn't get along with .NET's W^X JIT pages.
- **Web** — Next.js `output: "standalone"`, multi-stage `node:lts-alpine`, non-root, final image ≈ 150 MB.

### 2.2 Dokploy application setup (two apps, one monorepo)

| Setting | `portfolio-api` app | `portfolio-web` app |
|---|---|---|
| Source | GitHub repo, branch `main` | same repo, branch `main` |
| Trigger type | **`On Tag`**, not `On Push` | **`On Tag`** |
| Build | Dockerfile at `apps/api/Dockerfile`, context = **repo root** (`content/` must be in scope — `apps/api` as context gives `COPY ... not found`) | Dockerfile at `apps/web/Dockerfile`, context = `apps/web` |
| Build-time arguments | `GIT_SHA=dev` | `NEXT_PUBLIC_API_URL=https://api.emran.blog` (inlined into the client bundle — set as a runtime env var instead and the browser gets `undefined`) |
| Domain | `api.emran.blog` → container port `8080` | `emran.blog` → container port `3000` |
| HTTPS | Let's Encrypt via Traefik (automatic) | same |
| Env vars (runtime) | `ConnectionStrings__Db`, `Content__Root`, `Jwt__Issuer`/`Jwt__Audience`, `JWT_DEMO_KEY`, `RateLimiting__*`, `Cors__AllowedOrigins__0`, `DOTNET_EnableWriteXorExecute=0` | `API_BASE_URL=https://api.emran.blog` (server-side only — SSR/route handlers/`proxy.ts`) |
| Storage | **Bind mount** `/opt/data/portfolio-api` (host) → `/data` (container) — not a Dokploy volume, needs a fixed host path for the backup cron | — |
| Health check | `/api/health` — no traffic routed until healthy | `/` |

Dokploy's Trigger Type dropdown only offers `On Push`/`On Tag`, no true manual option — `On Tag` is the de-facto off-switch, since CI never pushes tags. Deploys are meant to happen only via the CI job in 2.3.

**Bind mount gotcha:** the container runs as non-root `app` (UID **1654** in the `aspnet:10.0-alpine` image — check with `docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0-alpine id app`). A fresh `mkdir`'d bind-mount host directory is root-owned, which overrides the image's own `/data` ownership and produces `SQLite Error 14: unable to open database file`. Fix: `chown -R 1654:1654 /opt/data/portfolio-api` before first deploy.

Traefik (bundled with Dokploy) is the single public entry point: it terminates TLS on 443 and routes by domain to the app containers, which expose no public ports. No `UseForwardedHeaders` middleware meant the API didn't trust `X-Forwarded-Proto` from Traefik, so it thought every request was `http` — leaked into the OpenAPI spec's server URL and got blocked as mixed content on `/docs`. Fixed with `app.UseForwardedHeaders(...)` in `Program.cs`, `KnownIPNetworks`/`KnownProxies` cleared since Traefik reaches the app over the Docker network, not loopback.

Full settings documented in `infra/dokploy.md` for reproducibility.

**Backup:** nightly cron on the VPS copying `/data/portfolio.db` off-box (it's one file — that's the point).

### 2.3 CI: tests gate the deploy (GitHub Actions + Dokploy API)

`api.yml`, path-filtered on `apps/api/**` and `content/**`:

1. **Test job** — `dotnet restore/build/test` on `Portfolio.slnx`. Red build = nothing deploys.
2. **Deploy job** (`needs: test`) — `POST /api/application.deploy` on Dokploy's API (`x-api-key` header, `applicationId` in body), then polls `/api/health` for `"status":"ok"`.

Deploy trigger went through two approaches: Dokploy's Webhooks-tab URL failed with `{"message":"Branch Not Match"}` — that endpoint expects a GitHub-shaped push payload (`ref: refs/heads/main`) to detect the branch, which a bare CI `curl POST` with no body can't supply. Switched to the direct API call (`applicationId` targets the app, no branch guessing needed) — that works.

Deploy verification originally tried to match the deployed `gitSha` (from `/api/health`) against the CI commit SHA. Dropped: Dokploy's Build-time Arguments are static per-app config, not passable dynamically through the deploy API call, so `GIT_SHA` can't be wired to the real commit per deploy without an extra API call to patch app config first. Settled for a plain `"status":"ok"` health check.

`web.yml` mirrors this (`npm run lint && npm run build` as the gate, `NEXT_PUBLIC_API_URL` set for the build so it validates against the real URL), reusing the same Dokploy API deploy call with a different `applicationId`, then polling `https://emran.blog/`.

**Repo secrets:** `DOKPLOY_API_URL` (panel domain, no scheme), `DOKPLOY_API_TOKEN` (from Dokploy profile → API/CLI settings), `DOKPLOY_APP_ID_API`, `DOKPLOY_APP_ID_WEB` (app IDs from `GET /api/project.all`). `JWT_DEMO_KEY` lives in Dokploy env config, not in GitHub. CI badge in this README and the site footer.

### 2.4 What Dokploy replaces vs. what stays yours

| Concern | Owned by | Notes |
|---|---|---|
| Reverse proxy, TLS certs, domain routing | Dokploy (Traefik) | Same role Caddy/Nginx would play — worth being able to explain (see the flagship article) |
| Image builds, deploys, restarts, volumes, env | Dokploy | The plumbing |
| Tests as a deploy gate | **You** (GitHub Actions) | A PaaS won't stop a broken commit; CI calls Dokploy's API only after tests pass |
| Health endpoint + deploy history | **You** (API) | Powers the status page's deploy history too |
| Dockerfiles, non-root images, healthchecks | **You** | Portable to any platform if Dokploy ever goes away |

---

## Phase 3 — Frontend rebuild (Next.js)

**Goal:** replace the shadcn-template look with a distinctive terminal/ops identity, rendered from the real API. **Milestone:** old site fully replaced, zero "Loading..." states, real metrics on the home page.

### 3.1 Design system

**Concept: "the site looks like the systems I build."** Terminal output, JSON, endpoint cards, monospace data, status colors. The design is the message.

| Token | Value | Usage |
|---|---|---|
| `--bg` | `#0B0E0D` (near-black, slight green cast) | Page background (dark-first, dark is default) |
| `--surface` | `#111514` | Cards, terminal window |
| `--border` | `#222826` | Hairline borders, 1px |
| `--text` | `#E6EAE8` | Primary text |
| `--text-dim` | `#8A938F` | Secondary text, comments |
| `--accent` | `#4ADE80` (terminal green) | Links, status-ok, prompt, ≤5% of any screen |
| `--warn` / `--err` | `#FBBF24` / `#F87171` | Metrics states only |

- **Typography:** JetBrains Mono for headings, nav, labels, all data/metrics; Inter for body/article prose. Two weights only (400/600). Loaded via `next/font` (self-hosted, zero layout shift).
- **Light mode:** supported via CSS variables flip, but dark is default — the audience lives in dark mode.
- **Density:** kill the giant empty hero. Max content width 1100px, tight vertical rhythm (sections ~64px apart, not 200px).
- **Anti-template rules:** no centered-heading+subtitle pattern repeated per section; vary layouts (left-aligned headers, full-bleed metric strip, two-column article list). No default shadcn pills/cards anywhere.

### 3.2 Component inventory (all hand-rolled, ~10 total)

`TerminalWindow` (chrome + blinking cursor + typed-out effect) · `EndpointCard` (method badge, path, status/latency, tech tags — the project card) · `MetricChip` (label + mono value + state color) · `Badge` · `Button` · `Nav` (with live status dot fed by `/api/health`) · `ArticleCard` · `CodeBlock` (Shiki, server-rendered) · `JsonViewer` (collapsible, for the playground) · `Footer`.

No component library. This folder **is** part of the portfolio — keep it clean.

### 3.3 Pages & data flow

| Route | Content | Data |
|---|---|---|
| `/` | Terminal hero (`$ curl api.emran.blog/api/whoami` typing out real JSON), live metric strip, 2 featured `EndpointCard`s, 3 latest articles | `whoami`, `metrics`, `projects`, `articles` |
| `/projects` | Endpoint-style cards, stack filter | `projects` |
| `/projects/[slug]` | Problem → architecture (diagram image) → trade-offs ("why X over Y") → outcome | `projects/{slug}` |
| `/playground` | Interactive API explorer (Phase 4) | live, client-side |
| `/status` | Real uptime, latency percentiles, error rate, per-endpoint table, deploy history w/ git SHAs | `metrics` (client refresh 30s) |
| `/notes` + `/notes/[slug]` | Articles, reading time, Shiki code blocks | `articles` |
| `/about` | Half the current length: experience, 4-line philosophy, link to "How this site works". Empty "Favorite Backend Challenges" section deleted. | static |

- **Rendering:** server components + `fetch(..., { next: { revalidate: 60 } })` (ISR) for all content — real HTML for crawlers, fixes the current SEO/loading problem. Client components only for: playground, status auto-refresh, hero typing effect.
- **API client:** one typed `lib/api.ts` mirroring the contract; response types shared by hand (small enough not to need codegen — note the trade-off: NSwag/openapi-ts is the "at scale" answer).
- **Packages beyond scaffold:** `shiki`. That's the list. No state library, no animation library (CSS only), no UI kit.

---

## Phase 4 — Differentiators

**Goal:** the two pages nobody else has. **Milestone:** a visitor can operate the API from the browser and watch real observability data.

### 4.1 `/playground` — interactive API explorer

- Endpoint picker (from the contract) → **Run** → live response: pretty JSON (`JsonViewer`), status code, response time, and the **actual rate-limit headers** from the API.
- **Auth demo flow:** ① call `GET /api/secret` → red **401** ② click "Get demo token" (`POST /api/auth/token`) → JWT shown with claims decoded and expiry countdown ③ re-run → green **200** with your claims echoed. Three clicks that demonstrate JWT issuance, validation, and claims better than a paragraph ever could.
- Copyable `curl` for every request; deliberately trip the rate limiter and show the **429** + `Retry-After` as a feature, not a bug.
- CORS on the API allows only `https://emran.blog`.

### 4.2 `/status` — real observability

- Fed entirely by `/api/metrics`: uptime, requests today, p50/p95, error rate, per-endpoint latency table, last 10 deploys (git SHA + timestamp from the `GIT_SHA` env). Auto-refresh 30s.
- Numbers are honest — if error rate is 0.3% because a crawler hit 404s, it says so. Honest beats impressive.

### 4.3 Hero touches

- Home hero types out the **real** `/api/whoami` response — the terminal isn't decoration, it's a live API call.
- Copyable `curl api.emran.blog/api/whoami` snippet: the site invites you to leave the browser.

---

## Phase 5 — Content + polish

**Goal:** the content that makes people stay, and the polish that makes it professional. Ongoing.

### 5.1 Flagship article: "How this portfolio works"

Architecture diagram → why SQLite over Postgres → why no Redis → why MediatR on only two endpoints → deploy pipeline walkthrough → what changes at 100× scale. **This one article does more than the other ten combined** — link it from the About page and the repo.

### 5.2 War story articles

Expand resume bullets into evidence: *"How I cut SQL Server query time 40%"* (real query plans, before/after, from ReliSource), *"Migrating .NET Framework → .NET 8 with the site staying up"* (strategy, rollback plan, what broke, from BizzNTek). Numbers + artifacts > adjectives.

### 5.3 Polish checklist

- **RSS/Atom** via a route handler (`/feed.xml`) — cheap, and the audience that matters uses it.
- **OG images** generated per page with `next/og` (terminal-styled, mono title on dark).
- **SEO:** metadata API per route, `sitemap.ts`, `robots.ts`; ISR already fixed the crawlability problem.
- **A11y & perf:** Lighthouse ≥ 95 across the board; `prefers-reduced-motion` disables the typing/cursor animation; visible focus states.
- **Repo presentation:** this README, CI badge, architecture diagram image, `LICENSE`, pinned on GitHub profile. The repo is a first-class portfolio artifact — recruiters open it.

---

## Design decisions & trade-offs

The section that matters most in interviews. What was chosen, what was rejected, and what changes at scale.

| Decision | Chosen | Rejected | Why | At 100× scale |
|---|---|---|---|---|
| Database | SQLite (file on volume) | PostgreSQL | Zero-ops; portfolio traffic is trivially within SQLite limits | Postgres + managed backups |
| Cache / rate limit | In-memory (`IMemoryCache`, built-in limiter) | Redis | Single instance ⇒ nothing distributed to coordinate; Redis here is cargo cult | Redis for shared cache + limiter state across replicas |
| Observability | Custom middleware → SQLite → `/api/metrics` | Prometheus + Grafana | 2 fewer containers; building it yourself is the better demo at this size | OTel → Prometheus → Grafana, alerting |
| CQRS | MediatR on 2 endpoints | MediatR everywhere | Pattern demonstrated without ceremony; 10-endpoint APIs don't need buses | Consistent CQRS as the team/domain grows |
| Auth | Demo JWT only; writes via git | Real user auth, admin CMS | Nothing to protect — content is public and version-controlled | Real IdP (Entra/Auth0) the moment a private resource exists |
| Deploy platform + proxy | Dokploy (Traefik, auto-TLS, push-to-deploy) | Hand-rolled compose + Caddy/Nginx + SSH scripts | A one-person project should spend effort on the application, not on reinventing a PaaS; the underlying flow (reverse proxy, TLS termination, health-gated deploys) is understood and documented, not just clicked | Raw pipeline (compose/K8s + registry + IaC) when the team or compliance needs demand it |
| Repo | Monorepo, path-filtered CI | Two repos | One story, one clone for reviewers; CI isolation preserved via path filters | Split when deploy cadences diverge |
| Frontend data | ISR (60s revalidate) | Client-side fetching (current site) | Real HTML for crawlers; fixes "Loading..." flash | On-demand revalidation via webhook on deploy |

---

## Local development

```bash
# API — http://localhost:8080 (docs at /docs)
cd apps/api && dotnet run --project src/Portfolio.Api

# Web — http://localhost:3000
cd apps/web && npm install && npm run dev   # API_BASE_URL=http://localhost:8080

# Full stack, production-shaped (local only — production is managed by Dokploy)
docker compose -f infra/docker-compose.local.yml up --build
```

Tests: `dotnet test` (apps/api) · `npm run lint && npm run build` (apps/web).

---

## Roadmap checklist

- [x] **Phase 0 — Decisions:** design tokens locked, 6 pages sketched, API contract written *(done — this README)*
- [x] **Phase 1 — Backend:** solution scaffolded → domain + ingestion → endpoints → metrics middleware → JWT demo + rate limiter → tests → `/docs`
- [x] **Phase 2 — Infra:** Dockerfiles → two Dokploy apps (`On Tag` trigger, domains, env, bind-mounted `/data`, health checks) → GitHub Actions tests gate a Dokploy-API-triggered deploy → `infra/dokploy.md` documented → CI badge *(done — live on VPS)*
- [x] **Phase 3 — Frontend:** tokens + fonts → 10 components → Home → Projects (+detail) → Notes → About → old site replaced *(built — replacement goes live with Phase 2 deploy)*
- [x] **Phase 4 — Differentiators:** `/playground` (+ JWT flow, 429 demo) → `/status` with deploy history → hero `curl` snippet *(built — ships with Phase 2 deploy)*
- [x] **Phase 5 — Content:** "How this portfolio works" → 2 war stories *(real numbers filled in from actual work history)* → RSS → OG images → Lighthouse ≥ 95 *(verify against web.emran.blog before cutover)* → repo polish

**Rule:** never start a phase before the previous one is deployed.

---

## Idea backlog (post-launch only)

Strictly **after Phase 5 ships**. Every item below is worth less than shipping the core — this list exists so good ideas have somewhere to wait instead of derailing the roadmap. Pace: at most one per month, each deployed and finished before the next.

### High-signal backend features

| Idea | What it demonstrates | Sketch |
|---|---|---|
| **Full-text search** *(built)* | Search architecture without over-tooling | `GET /api/search?q=` over articles/projects via **SQLite FTS5** — deliberately not Elasticsearch; fits the trade-offs philosophy |
| **Live request feed** *(built)* | Real-time (SignalR/SSE) — backs the resume claim | Status page streams anonymized live traffic via `GET /api/live/requests` (SSE): `GET /api/projects · 200 · 41ms` |
| **Load-test writeup** | Performance engineering, honest limits | Run **k6** against the API; publish RPS, p99 under load, and where SQLite taps out as an article with graphs |
| **API versioning demo** *(built)* | Evolution strategy | `GET /api/v2/projects` — paginated envelope vs. v1's bare array, explained in `/docs` |
| **Idempotency demo** | Distributed-systems hygiene | A playground POST honoring `Idempotency-Key` — retry it, get the same result, see why |
| **Self-hosted analytics** | Ops + privacy stance | **Umami/Plausible** as another Dokploy app; real visitor data, no Google |

*Three of seven above were built ahead of the "post-launch only" rule — pure code, no infra dependency, safe to ship whenever Phase 2 lands. Guestbook and its idempotency demo were built then pulled back out; load-test writeup and self-hosted analytics still need a deployed target and stay blocked.*

### Personality touches

| Idea | Why | Sketch |
|---|---|---|
| **curl-able homepage** | Developers screenshot this | Detect `curl` via User-Agent → ANSI-colored plain-text version of the site |
| **`GET /api/resume`** | "My resume is an API endpoint" | Structured JSON + link to the PDF |
| **Command palette** *(built)* | Fits the terminal concept | Cmd+K navigation, terminal-styled |
| **Changelog page** | Site documents its own evolution | `/changelog` fed by git tags/releases via the GitHub API |

### Moonshot

**`ssh emran.blog`** — a TUI version of the portfolio over SSH (Go: **Bubble Tea + Wish**). The terminal concept at its logical extreme, exercises the Go side of the stack, and essentially nobody has one. Only after everything above is stable.

**If only three ever get built:** FTS5 search, the live request feed, and the curl-able homepage — best signal-to-effort, and each deepens "the backend is the portfolio" instead of diluting it.

---

*© Emran. Built with .NET 10, Next.js, and a healthy suspicion of unnecessary infrastructure.*
