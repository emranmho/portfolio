// Typed client for the Portfolio API. Types are mirrored by hand from
// Portfolio.Application/Dtos.cs — small enough not to need codegen
// (NSwag/openapi-ts is the at-scale answer).

const API_BASE_URL = process.env.API_BASE_URL ?? "http://localhost:8080";

export interface Whoami {
  name: string;
  role: string;
  location: string;
  site: string;
  api: string;
  docs: string;
  github: string;
  email: string;
  focus: string[];
  message: string;
}

export interface Health {
  status: string;
  version: string;
  gitSha: string;
  startedAtUtc: string;
  uptimeSeconds: number;
}

export interface Project {
  slug: string;
  name: string;
  summary: string;
  description: string;
  stack: string[];
  repoUrl: string | null;
  liveUrl: string | null;
  featured: boolean;
}

export interface ArticleSummary {
  slug: string;
  title: string;
  summary: string;
  publishedAtUtc: string;
  tags: string[];
  readingTimeMinutes: number;
}

export interface ArticleDetail extends ArticleSummary {
  html: string;
  rawMarkdown: string;
}

export interface EndpointMetrics {
  route: string;
  count: number;
  errorCount: number;
  avgMs: number;
  p50Ms: number;
  p95Ms: number;
  p99Ms: number;
}

export interface Deploy {
  gitSha: string;
  deployedAtUtc: string;
}

export interface MetricsSummary {
  processStartedUtc: string;
  uptimeSeconds: number;
  requestsToday: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  errorRatePercent: number;
  endpoints: EndpointMetrics[];
  deploys: Deploy[];
}

async function get<T>(path: string, revalidate = 60): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    next: { revalidate },
  });
  if (!res.ok) {
    throw new ApiError(path, res.status);
  }
  return res.json() as Promise<T>;
}

// null-returning variant so pages can degrade gracefully instead of 500ing
// when the API is unreachable (e.g. local web dev without the API running).
async function tryGet<T>(path: string, revalidate = 60): Promise<T | null> {
  try {
    return await get<T>(path, revalidate);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) throw err;
    console.error(`API unreachable: GET ${path}`, err);
    return null;
  }
}

export class ApiError extends Error {
  constructor(
    public readonly path: string,
    public readonly status: number,
  ) {
    super(`GET ${path} → ${status}`);
  }
}

export const api = {
  baseUrl: API_BASE_URL,
  whoami: () => tryGet<Whoami>("/api/whoami"),
  health: () => tryGet<Health>("/api/health", 30),
  projects: (stack?: string) =>
    tryGet<Project[]>(`/api/projects${stack ? `?stack=${encodeURIComponent(stack)}` : ""}`),
  project: (slug: string) => tryGet<Project>(`/api/projects/${slug}`),
  articles: () => tryGet<ArticleSummary[]>("/api/articles"),
  article: (slug: string) => tryGet<ArticleDetail>(`/api/articles/${slug}`),
  metrics: () => tryGet<MetricsSummary>("/api/metrics", 30),
};
