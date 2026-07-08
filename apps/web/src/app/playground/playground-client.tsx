"use client";

import { useEffect, useMemo, useState } from "react";
import { PUBLIC_API_BASE_URL } from "@/lib/api";
import { JsonViewer } from "@/components/JsonViewer";
import { Badge } from "@/components/Badge";
import { Button } from "@/components/Button";
import { CopyButton } from "@/components/CopyButton";

interface Endpoint {
  method: "GET" | "POST";
  path: string;
  note: string;
}

// The public contract, minus /docs (a page, not JSON).
const ENDPOINTS: Endpoint[] = [
  { method: "GET", path: "/api/health", note: "liveness + version + git SHA" },
  { method: "GET", path: "/api/whoami", note: "identity payload (the hero terminal)" },
  { method: "GET", path: "/api/projects", note: "project list — try ?stack=dotnet" },
  { method: "GET", path: "/api/v2/projects", note: "v2: paginated envelope — versioning demo" },
  { method: "GET", path: "/api/articles", note: "article metadata, date desc" },
  { method: "GET", path: "/api/articles/hello-world", note: "CQRS/MediatR demo #1" },
  { method: "GET", path: "/api/search?q=sqlite", note: "full-text search — SQLite FTS5" },
  { method: "GET", path: "/api/metrics", note: "CQRS/MediatR demo #2 — real observability" },
  { method: "POST", path: "/api/auth/token", note: "issues a 15-min demo JWT" },
  { method: "GET", path: "/api/secret", note: "JWT-protected — 401 is the demo" },
];

const SECRET = ENDPOINTS.find((e) => e.path === "/api/secret")!;
const AUTH_TOKEN = ENDPOINTS.find((e) => e.path === "/api/auth/token")!;

interface RunResult {
  endpoint: Endpoint;
  status: number;
  ms: number;
  body: unknown;
  rateLimit: { limit: string | null; window: string | null; retryAfter: string | null };
  sentToken: boolean;
}

interface DemoToken {
  raw: string;
  claims: Record<string, unknown>;
  expiresAtUtc: string;
}

function decodeJwtPayload(token: string): Record<string, unknown> {
  try {
    const payload = token.split(".")[1];
    const b64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(atob(b64)) as Record<string, unknown>;
  } catch {
    return {};
  }
}

function curlFor(endpoint: Endpoint, token: string | null): string {
  const parts = ["curl -i"];
  if (endpoint.method === "POST") parts.push("-X POST");
  if (token && endpoint.path === "/api/secret")
    parts.push(`-H "Authorization: Bearer ${token}"`);
  parts.push(`${PUBLIC_API_BASE_URL}${endpoint.path}`);
  return parts.join(" ");
}

function statusTone(status: number): "accent" | "warn" | "err" {
  if (status < 400) return "accent";
  if (status === 429) return "warn";
  return "err";
}

export function PlaygroundClient() {
  const [selected, setSelected] = useState<Endpoint>(ENDPOINTS[0]);
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<RunResult | null>(null);
  const [token, setToken] = useState<DemoToken | null>(null);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const [tripReport, setTripReport] = useState<string | null>(null);
  const [tripping, setTripping] = useState(false);

  // Expiry countdown for the demo token.
  useEffect(() => {
    if (!token) {
      setSecondsLeft(null);
      return;
    }
    const tick = () => {
      const left = Math.floor(
        (new Date(token.expiresAtUtc).getTime() - Date.now()) / 1000,
      );
      setSecondsLeft(left);
      if (left <= 0) setToken(null);
    };
    tick();
    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [token]);

  const curl = useMemo(
    () => curlFor(selected, token?.raw ?? null),
    [selected, token],
  );

  async function run(endpoint: Endpoint): Promise<RunResult> {
    const headers: Record<string, string> = {};
    if (token && endpoint.path === "/api/secret")
      headers.Authorization = `Bearer ${token.raw}`;

    const started = performance.now();
    const res = await fetch(`${PUBLIC_API_BASE_URL}${endpoint.path}`, {
      method: endpoint.method,
      headers,
    });
    const ms = Math.round(performance.now() - started);

    let body: unknown;
    const text = await res.text();
    try {
      body = JSON.parse(text);
    } catch {
      body = text;
    }

    return {
      endpoint,
      status: res.status,
      ms,
      body,
      rateLimit: {
        limit: res.headers.get("X-RateLimit-Limit"),
        window: res.headers.get("X-RateLimit-Window"),
        retryAfter: res.headers.get("Retry-After"),
      },
      sentToken: "Authorization" in headers,
    };
  }

  async function handleRun(endpoint: Endpoint) {
    setSelected(endpoint);
    setRunning(true);
    try {
      const r = await run(endpoint);
      setResult(r);
      // Issuing a token via the picker also arms the auth demo.
      if (
        endpoint.path === "/api/auth/token" &&
        r.status === 200 &&
        typeof r.body === "object" &&
        r.body !== null
      ) {
        const b = r.body as { token?: string; expiresAtUtc?: string };
        if (b.token && b.expiresAtUtc) {
          setToken({
            raw: b.token,
            claims: decodeJwtPayload(b.token),
            expiresAtUtc: b.expiresAtUtc,
          });
        }
      }
    } catch {
      setResult({
        endpoint,
        status: 0,
        ms: 0,
        body: "API unreachable — is it running?",
        rateLimit: { limit: null, window: null, retryAfter: null },
        sentToken: false,
      });
    } finally {
      setRunning(false);
    }
  }

  // Deliberately exhaust the fixed-window limiter and show the 429.
  async function tripLimiter() {
    setTripping(true);
    setTripReport(null);
    const max = 30;
    for (let i = 1; i <= max; i++) {
      try {
        const r = await run({ method: "GET", path: "/api/whoami", note: "" });
        if (r.status === 429) {
          setResult(r);
          setTripReport(
            `Hit 429 on request #${i} — the limiter did its job. Retry-After: ${r.rateLimit.retryAfter}s. Your quota resets with the window; the playground shares it.`,
          );
          setTripping(false);
          return;
        }
      } catch {
        break;
      }
    }
    setTripReport(
      `Fired ${max} requests without a 429 — the window may have just reset. Run it again.`,
    );
    setTripping(false);
  }

  return (
    <div className="mt-10 grid gap-8 lg:grid-cols-[380px_minmax(0,1fr)]">
      {/* ── Left: endpoint picker + demos ── */}
      <div>
        <h2 className="mb-3 font-mono text-sm font-semibold text-text-dim">
          {"// endpoints"}
        </h2>
        <ul className="border border-border bg-surface">
          {ENDPOINTS.map((e) => (
            <li key={`${e.method} ${e.path}`} className="border-b border-border last:border-b-0">
              <button
                onClick={() => {
                  setSelected(e);
                  setResult(null);
                }}
                className={`flex w-full cursor-pointer items-baseline gap-2 px-3 py-2.5 text-left font-mono text-sm transition-colors hover:bg-bg ${
                  selected.path === e.path && selected.method === e.method
                    ? "bg-bg"
                    : ""
                }`}
              >
                <span
                  className={`w-11 shrink-0 text-xs font-semibold ${
                    e.method === "GET" ? "text-accent" : "text-warn"
                  }`}
                >
                  {e.method}
                </span>
                <span className="flex-1">
                  {e.path}
                  <span className="mt-0.5 block font-sans text-xs text-text-dim">
                    {e.note}
                  </span>
                </span>
              </button>
            </li>
          ))}
        </ul>

        {/* Auth demo — the three-click JWT lesson */}
        <h2 className="mt-8 mb-3 font-mono text-sm font-semibold text-text-dim">
          {"// auth demo — three clicks"}
        </h2>
        <div className="space-y-3 border border-border bg-surface p-4 font-mono text-sm">
          <p className="font-sans text-xs leading-relaxed text-text-dim">
            ① call <span className="font-mono">/api/secret</span> → 401 · ②
            get a token · ③ call it again → 200 with your claims. That
            contrast is JWT issuance, validation, and claims in one flow.
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              variant="ghost"
              onClick={() => handleRun(SECRET)}
              disabled={running}
            >
              ① GET /api/secret
            </Button>
            <Button
              variant="ghost"
              onClick={() => handleRun(AUTH_TOKEN)}
              disabled={running}
            >
              ② get demo token
            </Button>
          </div>
          {token && secondsLeft !== null && (
            <div className="border border-border bg-bg p-3 text-xs">
              <div className="flex items-center justify-between gap-2">
                <span className="text-accent">token armed</span>
                <span className={secondsLeft < 60 ? "text-warn" : "text-text-dim"}>
                  expires in {Math.floor(secondsLeft / 60)}m {secondsLeft % 60}s
                </span>
              </div>
              <div className="mt-2 break-all text-text-dim">
                {token.raw.slice(0, 48)}…
              </div>
              <div className="mt-2 text-text-dim">decoded claims:</div>
              <pre className="mt-1 overflow-x-auto whitespace-pre text-text">
                {JSON.stringify(token.claims, null, 2)}
              </pre>
              <p className="mt-2 font-sans text-text-dim">
                Now re-run ① — the same endpoint answers 200.
              </p>
            </div>
          )}
        </div>

        {/* Rate limiter demo */}
        <h2 className="mt-8 mb-3 font-mono text-sm font-semibold text-text-dim">
          {"// rate limiter — trip it on purpose"}
        </h2>
        <div className="space-y-3 border border-border bg-surface p-4">
          <p className="font-sans text-xs leading-relaxed text-text-dim">
            The API allows a fixed window of requests per IP. This fires
            requests until one comes back <span className="font-mono text-warn">429</span> —
            the headers and <span className="font-mono">Retry-After</span> are
            the feature, not a bug.
          </p>
          <Button variant="ghost" onClick={tripLimiter} disabled={tripping || running}>
            {tripping ? "firing…" : "trip the limiter"}
          </Button>
          {tripReport && (
            <p className="font-sans text-xs leading-relaxed text-warn">{tripReport}</p>
          )}
        </div>
      </div>

      {/* ── Right: request + live response ── */}
      <div>
        <h2 className="mb-3 font-mono text-sm font-semibold text-text-dim">
          {"// request"}
        </h2>
        <div className="flex flex-wrap items-center gap-3 border border-border bg-surface p-4">
          <code className="min-w-0 flex-1 break-all font-mono text-sm text-text">
            {curl}
          </code>
          <CopyButton text={curl} label="copy curl" />
          <Button onClick={() => handleRun(selected)} disabled={running}>
            {running ? "running…" : "run ▸"}
          </Button>
        </div>

        <h2 className="mt-8 mb-3 font-mono text-sm font-semibold text-text-dim">
          {"// response"}
        </h2>
        <div aria-live="polite">
        {result ? (
          <div>
            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 border border-b-0 border-border bg-surface px-4 py-3 font-mono text-sm">
              <Badge tone={statusTone(result.status)}>
                {result.status === 0 ? "ERR" : result.status}
              </Badge>
              <span className="text-text-dim">{result.ms}ms</span>
              {result.sentToken && <Badge tone="accent">Bearer sent</Badge>}
              {result.rateLimit.limit && (
                <span className="text-xs text-text-dim">
                  X-RateLimit-Limit: <span className="text-text">{result.rateLimit.limit}</span>
                  {" · "}X-RateLimit-Window:{" "}
                  <span className="text-text">{result.rateLimit.window}</span>
                </span>
              )}
              {result.rateLimit.retryAfter && (
                <span className="text-xs text-warn">
                  Retry-After: {result.rateLimit.retryAfter}s
                </span>
              )}
            </div>
            {typeof result.body === "string" ? (
              <pre className="overflow-x-auto border border-border bg-surface p-4 font-mono text-sm text-err">
                {result.body}
              </pre>
            ) : (
              <JsonViewer data={result.body as never} />
            )}
            {result.endpoint.path === "/api/secret" && result.status === 401 && (
              <p className="mt-3 font-sans text-sm leading-relaxed text-text-dim">
                <span className="text-err">401</span> — exactly right. No token,
                no entry. Click{" "}
                <span className="font-mono text-accent">② get demo token</span>{" "}
                and run it again.
              </p>
            )}
          </div>
        ) : (
          <p className="border border-border bg-surface p-4 font-mono text-sm text-text-dim">
            Pick an endpoint and hit run — the response, latency, and real
            rate-limit headers land here.
          </p>
        )}
        </div>
      </div>
    </div>
  );
}
