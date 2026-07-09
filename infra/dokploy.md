# Dokploy setup

Two apps in Dokploy, one repo. Deploys are triggered only by CI (`.github/workflows/api.yml`,
`web.yml`) calling Dokploy's API directly — Trigger Type is set to **`On Tag`** (not
`On Push`) for both apps, since Dokploy's dropdown has no true manual option and CI never
pushes tags. This keeps a red test run from ever reaching production.

## portfolio-api

| Setting | Value |
|---|---|
| Source | GitHub repo, branch `main` |
| Trigger type | `On Tag` |
| Build type | Dockerfile |
| Dockerfile path | `apps/api/Dockerfile` |
| Build context | repo root (so `content/` is in scope — `apps/api` gives `COPY ... not found`) |
| Build-time argument | `GIT_SHA=dev` (static placeholder — see "SHA verification" below) |
| Domain | `api.emran.blog` → container port `8080` |
| HTTPS | Let's Encrypt via Traefik (automatic) |
| Storage | **Bind mount**: host `/opt/data/portfolio-api` → container `/data` (not a Dokploy volume — needs a fixed host path for the backup cron) |
| Health check path | `/api/health` |

Bind mount gotcha: the API container runs as non-root `app` (UID **1654** in
`aspnet:10.0-alpine` — confirm with `docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0-alpine id app`).
A freshly `mkdir`'d host directory is root-owned, which overrides the image's own `/data`
ownership and produces `SQLite Error 14: unable to open database file`. Fix before first
deploy:

```bash
mkdir -p /opt/data/portfolio-api
chown -R 1654:1654 /opt/data/portfolio-api
```

Env vars (runtime):

```
ConnectionStrings__Db=Data Source=/data/portfolio.db
Content__Root=/content
Jwt__Issuer=https://api.emran.blog
Jwt__Audience=https://emran.blog
JWT_DEMO_KEY=<secret, generated once>
RateLimiting__PermitLimit=20
RateLimiting__WindowSeconds=60
Cors__AllowedOrigins__0=https://emran.blog
DOTNET_EnableWriteXorExecute=0
```

`DOTNET_EnableWriteXorExecute=0` is required on this VPS — without it the container exits
instantly with code `139` (SIGSEGV, no logs); the kernel/hypervisor doesn't get along with
.NET's W^X JIT pages. `JWT_DEMO_KEY` must be spelled exactly that way (flat key, not nested
under `Jwt:`) — the app throws on startup otherwise.

## portfolio-web

| Setting | Value |
|---|---|
| Source | same repo, branch `main` |
| Trigger type | `On Tag` |
| Build type | Dockerfile |
| Dockerfile path | `apps/web/Dockerfile` |
| Build context | `apps/web` |
| Build-time argument | `NEXT_PUBLIC_API_URL=https://api.emran.blog` (inlined into the client bundle at build; a runtime env var here means the browser gets `undefined`) |
| Domain | `emran.blog` → container port `3000` |
| HTTPS | Let's Encrypt via Traefik (automatic) |
| Health check path | `/` |

Env vars:

```
API_BASE_URL=https://api.emran.blog
```

(Server-side fetches go over the public domain rather than Dokploy's internal network —
simpler than looking up the api container's internal name, and the latency cost is
negligible.)

## Deploy trigger (used by CI)

CI triggers deploys via Dokploy's API (`POST /api/application.deploy`), not the
Webhooks-tab URL — that URL expects a GitHub-shaped push payload (`ref: refs/heads/main`)
to determine the branch, which a bare CI curl can't supply, so it fails with
`{"message":"Branch Not Match"}`.

Setup:

1. Dokploy → profile/settings → API/CLI section → generate an API token.
2. Get each app's `applicationId`: `curl -s https://vps.emran.blog/api/project.all -H "x-api-key: <token>" | jq`.
3. GitHub repo secrets:
   - `DOKPLOY_API_URL` → `vps.emran.blog` (panel domain, no `https://`)
   - `DOKPLOY_API_TOKEN` → the token from step 1
   - `DOKPLOY_APP_ID_API` → portfolio-api's `applicationId`
   - `DOKPLOY_APP_ID_WEB` → portfolio-web's `applicationId`

`JWT_DEMO_KEY` lives only in Dokploy's env config, never in GitHub.

## SHA verification (dropped)

CI originally tried to match the deployed `gitSha` (from `/api/health`) against the commit
that triggered the workflow. Dropped: Dokploy's Build-time Arguments are static per-app
config, not passable dynamically through `POST /api/application.deploy` — wiring the real
SHA through would need an extra API call to patch app config before every deploy. CI just
polls for `"status":"ok"` instead.

## Forwarded headers (API)

Traefik terminates TLS and proxies to the container over the Docker network (not loopback),
so ASP.NET's default trusted-proxy list doesn't include it. Without `UseForwardedHeaders` the
API doesn't trust `X-Forwarded-Proto`, thinks every request is `http`, and leaks that into the
OpenAPI spec's server URL — browsers block `/docs` fetching `http://` from an `https://` page
as mixed content. Fixed in `Program.cs` with `UseForwardedHeaders(...)`,
`KnownIPNetworks`/`KnownProxies` cleared.

## Backup

Nightly cron on the VPS copying `/data/portfolio.db` off-box (it's one file).

## Rollback

Redeploy the previous good commit from the Dokploy UI, or re-fire
`POST /api/application.deploy` once `main` is reset/reverted to that commit.
