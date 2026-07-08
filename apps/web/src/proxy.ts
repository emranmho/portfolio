import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// Proxy is invoked outside the render pipeline (see Next 16 docs) — no shared
// modules/globals, so this duplicates the fetch logic in lib/api.ts on purpose.
const API_BASE_URL = process.env.API_BASE_URL ?? "http://localhost:8080";

const RESET = "\x1b[0m";
const BOLD = "\x1b[1m";
const DIM = "\x1b[2m";
const CYAN = "\x1b[36m";
const GREEN = "\x1b[32m";
const YELLOW = "\x1b[33m";

interface Whoami {
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

interface MetricsSummary {
  uptimeSeconds: number;
  requestsToday: number;
  p95LatencyMs: number;
  errorRatePercent: number;
}

async function fetchJson<T>(path: string): Promise<T | null> {
  try {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      signal: AbortSignal.timeout(2000),
    });
    if (!res.ok) return null;
    return (await res.json()) as T;
  } catch {
    return null;
  }
}

function formatUptime(seconds: number): string {
  const days = Math.floor(seconds / 86400);
  const hours = Math.floor((seconds % 86400) / 3600);
  if (days > 0) return `${days}d ${hours}h`;
  const minutes = Math.floor((seconds % 3600) / 60);
  return `${hours}h ${minutes}m`;
}

function render(whoami: Whoami | null, metrics: MetricsSummary | null): string {
  const lines: string[] = [];

  lines.push(`${BOLD}${CYAN}$ whoami${RESET}`);

  if (whoami) {
    lines.push(`${BOLD}${whoami.name}${RESET} ${DIM}—${RESET} ${whoami.role}`);
    lines.push(`${DIM}${whoami.location}${RESET}`);
    lines.push("");
    lines.push(whoami.message);
    lines.push("");
    lines.push(`${GREEN}focus${RESET}    ${whoami.focus.join(", ")}`);
    lines.push(`${GREEN}site${RESET}     ${whoami.site}`);
    lines.push(`${GREEN}api${RESET}      ${whoami.api}`);
    lines.push(`${GREEN}docs${RESET}     ${whoami.docs}`);
    lines.push(`${GREEN}github${RESET}   ${whoami.github}`);
    lines.push(`${GREEN}email${RESET}    ${whoami.email}`);
    lines.push(`${GREEN}resume${RESET}   ${whoami.site}/resume.pdf ${DIM}(or curl ${whoami.api}/api/resume)${RESET}`);
  } else {
    lines.push(`${YELLOW}Mohammodullah Emran${RESET} ${DIM}—${RESET} Software Engineer — SRE`);
    lines.push(`${DIM}${API_BASE_URL} unreachable — static fallback.${RESET}`);
  }

  if (metrics) {
    lines.push("");
    lines.push(`${BOLD}${CYAN}// live from /api/metrics${RESET}`);
    lines.push(
      `uptime ${formatUptime(metrics.uptimeSeconds)}  ` +
        `requests today ${metrics.requestsToday}  ` +
        `p95 ${metrics.p95LatencyMs.toFixed(0)}ms  ` +
        `errors ${metrics.errorRatePercent.toFixed(2)}%`,
    );
  }

  lines.push("");
  lines.push(`${DIM}The backend is the portfolio. Browser version: ${whoami?.site ?? "https://emran.blog"}${RESET}`);
  lines.push("");

  return lines.join("\n");
}

export async function proxy(request: NextRequest) {
  if (request.nextUrl.pathname !== "/") {
    return NextResponse.next();
  }

  const userAgent = request.headers.get("user-agent") ?? "";
  if (!/curl/i.test(userAgent)) {
    return NextResponse.next();
  }

  const [whoami, metrics] = await Promise.all([
    fetchJson<Whoami>("/api/whoami"),
    fetchJson<MetricsSummary>("/api/metrics"),
  ]);

  return new NextResponse(render(whoami, metrics), {
    status: 200,
    headers: { "content-type": "text/plain; charset=utf-8" },
  });
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)"],
};
