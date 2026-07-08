"use client";

import { useEffect, useState } from "react";
import { PUBLIC_API_BASE_URL } from "@/lib/api";
import { Badge } from "@/components/Badge";

interface LiveRequestEvent {
  method: string;
  route: string;
  status: number;
  elapsedMs: number;
  timestampUtc: string;
}

interface Row extends LiveRequestEvent {
  id: number;
}

const MAX_ROWS = 20;
let nextId = 0;

function statusTone(status: number): "accent" | "warn" | "err" {
  if (status < 400) return "accent";
  if (status === 429) return "warn";
  return "err";
}

export function LiveFeedPanel() {
  const [events, setEvents] = useState<Row[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const source = new EventSource(`${PUBLIC_API_BASE_URL}/api/live/requests`);
    source.addEventListener("open", () => setConnected(true));
    source.addEventListener("error", () => setConnected(false));
    source.addEventListener("request", (e) => {
      const data = JSON.parse((e as MessageEvent<string>).data) as LiveRequestEvent;
      setEvents((prev) => [{ ...data, id: nextId++ }, ...prev].slice(0, MAX_ROWS));
    });
    return () => source.close();
  }, []);

  return (
    <section className="mt-12">
      <div className="mb-4 flex items-baseline justify-between">
        <h2 className="font-mono text-lg font-semibold">Live request feed</h2>
        <span className="flex items-center gap-2 font-mono text-xs text-text-dim">
          <span
            className={`inline-block h-2 w-2 rounded-full ${
              connected ? "bg-accent" : "bg-err"
            }`}
          />
          {connected ? "streaming via SSE" : "connecting…"}
        </span>
      </div>
      <p className="mb-3 font-sans text-xs leading-relaxed text-text-dim">
        Every request anyone makes to the API — including yours, right now —
        shows up here in real time. Anonymized: route template, status, and
        latency only.{" "}
        <a
          href={`${PUBLIC_API_BASE_URL}/api/live/requests`}
          className="text-accent hover:underline"
        >
          Open the raw stream
        </a>
        .
      </p>
      <div className="max-h-80 overflow-y-auto border border-border bg-surface">
        {events.length === 0 ? (
          <p className="p-4 font-mono text-sm text-text-dim">
            Waiting for traffic — hit the{" "}
            <a href="/playground" className="text-accent hover:underline">
              playground
            </a>{" "}
            to see it live.
          </p>
        ) : (
          <ul className="font-mono text-sm">
            {events.map((e) => (
              <li
                key={e.id}
                className="flex items-baseline gap-3 border-b border-border px-3 py-2 last:border-b-0"
              >
                <Badge tone={statusTone(e.status)}>{e.status}</Badge>
                <span className="text-text-dim">{e.method}</span>
                <span className="flex-1 truncate">{e.route}</span>
                <span className="text-text-dim">{Math.round(e.elapsedMs)}ms</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
