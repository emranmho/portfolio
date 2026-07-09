# Dokploy setup

Two apps in Dokploy, one repo. Deploys are triggered only by CI (`.github/workflows/api.yml`,
`web.yml`) — Dokploy's own auto-deploy-on-push must be **disabled** for both apps so a red
test run can never reach production.

## portfolio-api

| Setting | Value |
|---|---|
| Source | GitHub repo, branch `main` |
| Build type | Dockerfile |
| Dockerfile path | `apps/api/Dockerfile` |
| Build context | repo root (so `content/` is in scope) |
| Build-time argument | `GIT_SHA=${COMMIT_SHA}` |
| Domain | `api.emran.blog` → container port `8080` |
| HTTPS | Let's Encrypt via Traefik (automatic) |
| Volume | `/data` → persistent volume |
| Health check path | `/api/health` |
| Trigger type | **manual** (CI triggers via webhook instead) |

Env vars:

```
ConnectionStrings__Db=Data Source=/data/portfolio.db
JWT_DEMO_KEY=<secret, generated once>
Cors__AllowedOrigins__0=https://emran.blog
```

## portfolio-web

| Setting | Value |
|---|---|
| Source | same repo, branch `main` |
| Build type | Dockerfile |
| Dockerfile path | `apps/web/Dockerfile` |
| Build context | `apps/web` |
| Build args | `NEXT_PUBLIC_API_URL=https://api.emran.blog` |
| Domain | `emran.blog` → container port `3000` |
| HTTPS | Let's Encrypt via Traefik (automatic) |
| Health check path | `/` |
| Auto deploy on push | **disabled** |

Env vars:

```
API_BASE_URL=http://<portfolio-api container name on the Dokploy network>:8080
```

## Deploy webhooks (used by CI)

In each Dokploy app: Settings → Webhooks → generate a deploy webhook URL. Store them as
GitHub repo secrets:

- `DOKPLOY_WEBHOOK_API` → portfolio-api's webhook URL
- `DOKPLOY_WEBHOOK_WEB` → portfolio-web's webhook URL

`JWT_DEMO_KEY` lives only in Dokploy's env config, never in GitHub.

## Backup

Nightly cron on the VPS copying `/data/portfolio.db` off-box (it's one file).

## Rollback

Redeploy the previous good commit from the Dokploy UI, or re-fire the deploy webhook once
`main` is reset/reverted to that commit's SHA.
