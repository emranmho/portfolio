---
title: "Hello, world — why this site is an API"
summary: "The first post: what it means that the backend is the portfolio, and how publishing works here (spoiler: git push)."
date: 2026-07-01
tags: [meta, dotnet]
---

# Hello, world

This site is a thin client of a real, observable .NET API. Every article you read here —
including this one — lives in the repo as a markdown file with YAML frontmatter. At startup,
the API parses it with Markdig, renders the HTML you're reading, and upserts it into SQLite.

Publishing is `git push`. There is no CMS, no admin panel, no write endpoint. The deploy
pipeline (tests gate, then Dokploy builds and ships) is the publishing workflow.

## Why bother?

Because a resume that says "observability, CI/CD, clean architecture" proves nothing.
A status page showing real p95 latency does. Check it out:

```bash
curl https://api.emran.blog/api/metrics
```

More on the architecture in the flagship article: *How this portfolio works*.
