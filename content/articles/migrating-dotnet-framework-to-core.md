---
title: "Migrating .NET Framework → .NET 8 with the site staying up"
summary: "Strangler-style migration of a production system at BizzNTek: the strategy, the rollback plan, what broke anyway, and how traffic never noticed."
date: 2026-07-08
tags: [dotnet, migration, war-story]
---

# Migrating .NET Framework → .NET 8 with the site staying up

"We migrated to .NET Core" is table stakes. The interesting questions are: in what order,
with what rollback plan, and what broke anyway. This is that story from leading the
migration of a legacy .NET Framework web application to .NET 8 at BizzNTek — a live,
customer-facing system, not a greenfield rewrite.

## Why migrate at all

.NET Framework 4.x is Windows-only, IIS-bound, and effectively frozen. The concrete costs
we were paying: Windows licensing on every node, no containerization path, a cold-start
and memory profile that made scaling expensive, and a growing list of libraries whose new
versions were netstandard/net-only. The trigger that turned this from "someday" into a
funded project was the Azure hosting bill itself — Windows-only App Service plans cost
more than the Linux containers the rest of the stack was already moving to, and that gap
only widens as traffic grows.

## The strategy: strangler, not rewrite

The cardinal rule: **the migration must never be the reason the system is down.** That
rules out big-bang. The approach:

1. **Inventory by dependency, not by size.** Every project graded on its blockers:
   `System.Web` coupling, WCF, third-party packages without netstandard targets, Windows
   auth, registry/GAC assumptions. The grade — not business importance — set the order.
2. **Shared code to `netstandard2.0` first.** Class libraries were retargeted while still
   consumed by the Framework host. Weeks of work shipped to production with zero runtime
   change — the safest possible way to burn down the biggest risk.
3. **Route-by-route strangler at the reverse proxy.** The new Core host went up beside
   the old one behind the same proxy. Endpoints moved one route group at a time; the
   proxy decided who served what. Rollback for any route = one config line, seconds, no
   deploy.
4. **The database didn't move.** Same schema, same data, both hosts pointing at it. One
   migration at a time — runtime *or* storage, never both.

## The rollback plan is the deliverable

Every cutover step had a written, tested rollback *before* it ran:

- Proxy config in git; reverting a route was a one-line revert, applied in seconds.
- Both hosts ran identical health endpoints; the proxy only sent traffic to green.
- Session/auth state was made host-agnostic *before* any route moved: both hosts shared the
  same ASP.NET Core Data Protection keys and cookie format, so a user could bounce between
  the old Framework host and the new Core host mid-session without noticing or getting
  logged out.

The system stayed up through the whole program — the same production monitoring that
predated the migration kept running unchanged throughout, so uptime was verified against a
consistent baseline, not a retrospective estimate stitched together after the fact.

## What broke anyway

The honest section. Things the inventory missed:

- **Config binding semantics.** `web.config` → `appsettings.json` looks mechanical, until
  code that read `ConfigurationManager.AppSettings` at static-initialization time started
  seeing nulls under the new host's startup order. Found in staging; fixed by pushing all
  config reads behind injected options.
- **Culture-sensitive string behavior.** .NET Framework on Windows uses the OS's NLS
  globalization tables; .NET on Linux uses ICU by default. String sorting and casing for
  the same culture can come out in a different order on the two hosts — a silent
  data-ordering difference, caught by a diff-testing harness replaying production reads
  against both hosts before any route serving sorted lists was moved.
- **The route-level rollback earned its keep.** A handful of cutovers surfaced regressions
  in staging traffic replay that would otherwise have reached production — each one was a
  config-binding or globalization mismatch like the two above, not a novel failure mode.
  None needed a full deploy to fix or revert: flip the route back at the proxy, fix forward,
  move it again.

## What I'd generalize

- **Order by blocker, not by value.** The scariest dependency defines the critical path.
- **Never migrate runtime and storage in the same step.**
- **Rollback that needs a deploy isn't rollback.** Proxy-level cutover made reverting
  cheaper than debating whether to revert.
- **Diff-test reads before cutting writes.** Replaying real traffic against both stacks
  finds the disagreements no test suite was written to catch.

The same philosophy runs this site: [health-gated deploys, SHA verification, and rollback
as a first-class step](/notes/how-this-portfolio-works) — just at portfolio scale.
