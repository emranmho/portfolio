---
title: "How this portfolio works"
summary: "Architecture walkthrough: why SQLite over Postgres, why no Redis, why MediatR on only two endpoints, and what changes at 100× scale."
date: 2026-07-05
tags: [architecture, dotnet, trade-offs]
---

# How this portfolio works

> Draft placeholder — the full flagship article ships in Phase 5. The endpoint serving it
> is already real, which is rather the point.

The system is two containers on one VPS behind Traefik: a Next.js frontend and this .NET 10
API. Content lives in git, metrics live in SQLite, and every number on the status page is
measured by middleware in this codebase.

## The short version of every trade-off

- **SQLite over Postgres** — zero ops, one file, trivially handles portfolio traffic.
- **No Redis** — single instance means nothing distributed to coordinate.
- **MediatR on exactly two endpoints** — the pattern demonstrated without the ceremony.
- **Custom metrics middleware over Prometheus + Grafana** — two fewer containers, and
  building it is the better demonstration at this size.

Each of these flips at scale, and knowing where the flip point is matters more than
picking the "serious" tool on day one.
