"use client";

import { useEffect, useState } from "react";
import { PUBLIC_API_BASE_URL, type MetricsSummary } from "@/lib/api";
import { MetricChip } from "@/components/MetricChip";
import { formatCount, formatDate, formatMs, formatUptime } from "@/lib/format";

const REFRESH_MS = 30_000;

export function StatusClient({ initial }: { initial: MetricsSummary | null }) {
  const [metrics, setMetrics] = useState<MetricsSummary | null>(initial);
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function refresh() {
      try {
        const res = await fetch(`${PUBLIC_API_BASE_URL}/api/metrics`);
        if (!res.ok) return;
        const data = (await res.json()) as MetricsSummary;
        if (!cancelled) {
          setMetrics(data);
          setUpdatedAt(new Date());
        }
      } catch {
        // API unreachable — keep showing the last good numbers.
      }
    }
    refresh();
    const timer = setInterval(refresh, REFRESH_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, []);

  if (!metrics) {
    return (
      <p className="mt-10 border border-border bg-surface p-4 font-mono text-sm text-err">
        /api/metrics unreachable — which is itself a status report.
      </p>
    );
  }

  return (
    <div className="mt-10">
      <div className="mb-4 flex items-baseline justify-between">
        <h2 className="font-mono text-sm font-semibold text-text-dim">
          {"// live from /api/metrics — refreshes every 30s"}
        </h2>
        {updatedAt && (
          <span className="font-mono text-xs text-text-dim">
            updated {updatedAt.toLocaleTimeString()}
          </span>
        )}
      </div>

      <div className="grid grid-cols-2 gap-px border border-border bg-border sm:grid-cols-5 [&>*]:border-0">
        <MetricChip
          label="uptime"
          value={formatUptime(metrics.uptimeSeconds)}
          state="ok"
        />
        <MetricChip
          label="requests today"
          value={formatCount(metrics.requestsToday)}
        />
        <MetricChip label="avg latency" value={formatMs(metrics.avgLatencyMs)} />
        <MetricChip
          label="p95 latency"
          value={formatMs(metrics.p95LatencyMs)}
          state={metrics.p95LatencyMs > 500 ? "warn" : "ok"}
        />
        <MetricChip
          label="error rate"
          value={`${metrics.errorRatePercent.toFixed(2)}%`}
          state={metrics.errorRatePercent > 1 ? "warn" : "ok"}
        />
      </div>

      {/* Per-endpoint breakdown */}
      <section className="mt-12">
        <h2 className="mb-4 font-mono text-lg font-semibold">Per endpoint</h2>
        {metrics.endpoints.length > 0 ? (
          <div className="overflow-x-auto border border-border">
            <table className="w-full border-collapse font-mono text-sm">
              <thead>
                <tr className="border-b border-border bg-surface text-left text-xs text-text-dim">
                  <th className="px-3 py-2 font-semibold">route</th>
                  <th className="px-3 py-2 text-right font-semibold">count</th>
                  <th className="px-3 py-2 text-right font-semibold">errors</th>
                  <th className="px-3 py-2 text-right font-semibold">avg</th>
                  <th className="px-3 py-2 text-right font-semibold">p50</th>
                  <th className="px-3 py-2 text-right font-semibold">p95</th>
                  <th className="px-3 py-2 text-right font-semibold">p99</th>
                </tr>
              </thead>
              <tbody>
                {metrics.endpoints.map((e) => (
                  <tr key={e.route} className="border-b border-border last:border-b-0">
                    <td className="px-3 py-2">{e.route}</td>
                    <td className="px-3 py-2 text-right text-text-dim">
                      {formatCount(e.count)}
                    </td>
                    <td
                      className={`px-3 py-2 text-right ${
                        e.errorCount > 0 ? "text-warn" : "text-text-dim"
                      }`}
                    >
                      {formatCount(e.errorCount)}
                    </td>
                    <td className="px-3 py-2 text-right">{formatMs(e.avgMs)}</td>
                    <td className="px-3 py-2 text-right">{formatMs(e.p50Ms)}</td>
                    <td className="px-3 py-2 text-right">{formatMs(e.p95Ms)}</td>
                    <td className="px-3 py-2 text-right">{formatMs(e.p99Ms)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="border border-border bg-surface p-4 font-mono text-sm text-text-dim">
            No samples in the current window yet — hit the{" "}
            <a href="/playground" className="text-accent hover:underline">
              playground
            </a>{" "}
            and come back.
          </p>
        )}
        <p className="mt-2 font-mono text-xs text-text-dim">
          /api/health is excluded — the Docker healthcheck shouldn&apos;t inflate
          traffic.
        </p>
      </section>

      {/* Deploy history */}
      <section className="mt-12">
        <h2 className="mb-4 font-mono text-lg font-semibold">Deploys</h2>
        {metrics.deploys.length > 0 ? (
          <ol className="border border-border bg-surface font-mono text-sm">
            {metrics.deploys.slice(0, 10).map((d, i) => (
              <li
                key={`${d.gitSha}-${d.deployedAtUtc}`}
                className="flex items-baseline gap-4 border-b border-border px-4 py-2.5 last:border-b-0"
              >
                <span className={i === 0 ? "text-accent" : "text-text-dim"}>
                  {d.gitSha.slice(0, 7)}
                </span>
                <span className="text-text-dim">
                  {formatDate(d.deployedAtUtc)}{" "}
                  {new Date(d.deployedAtUtc).toLocaleTimeString()}
                </span>
                {i === 0 && <span className="text-xs text-accent">← running</span>}
              </li>
            ))}
          </ol>
        ) : (
          <p className="border border-border bg-surface p-4 font-mono text-sm text-text-dim">
            No deploy history yet — it starts recording with the first Dokploy
            deploy (Phase 2).
          </p>
        )}
      </section>
    </div>
  );
}
