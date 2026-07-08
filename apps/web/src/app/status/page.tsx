import type { Metadata } from "next";
import { api } from "@/lib/api";
import { StatusClient } from "./status-client";

export const metadata: Metadata = {
  title: "Status",
  description:
    "Real observability: uptime, latency percentiles, error rate, per-endpoint breakdown, and deploy history — measured by the API's own middleware.",
};

export default async function StatusPage() {
  // Server-rendered first paint (ISR 30s); the client refreshes from the
  // browser every 30s after hydration.
  const initial = await api.metrics();

  return (
    <div className="py-12">
      <header className="max-w-2xl">
        <p className="font-mono text-sm text-accent">$ systemctl status api</p>
        <h1 className="mt-2 font-mono text-3xl font-semibold">Status</h1>
        <p className="mt-3 leading-relaxed text-text-dim">
          Every number below is measured by middleware in the API itself and
          persisted to SQLite — no Prometheus, no Grafana, no vendor. If the
          error rate is 0.3% because a crawler hit 404s, it says so. Honest
          beats impressive.
        </p>
      </header>
      <StatusClient initial={initial} />
    </div>
  );
}
