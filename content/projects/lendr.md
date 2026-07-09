---
name: LendR
summary: "Lending/borrowing tracker with dual transaction modes, built on Vertical Slice Architecture."
stack: [dotnet, react, typescript, postgresql, signalr, docker]
repoUrl:
liveUrl:
featured: false
order: 3
---

A .NET 10 + React 19 app for tracking who owes what, built on Vertical Slice Architecture (FastEndpoints + MediatR) instead of the usual layered setup — each feature owns its own request/response/handler slice. Real-time SignalR notifications keep both sides of a transaction in sync, storage is pluggable between Oracle OCI object storage and local disk, and it runs self-hosted on a VPS.
