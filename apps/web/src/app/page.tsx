import Link from "next/link";
import { api } from "@/lib/api";
import { TerminalWindow } from "@/components/TerminalWindow";
import { EndpointCard } from "@/components/EndpointCard";
import { ArticleCard } from "@/components/ArticleCard";
import { MetricChip } from "@/components/MetricChip";
import { Button } from "@/components/Button";
import { CopyButton } from "@/components/CopyButton";
import { formatCount, formatMs, formatUptime } from "@/lib/format";

export default async function HomePage() {
  const [whoami, metrics, projects, articles] = await Promise.all([
    api.whoami(),
    api.metrics(),
    api.projects(),
    api.articles(),
  ]);

  const featured = (projects ?? []).filter((p) => p.featured).slice(0, 2);
  const latest = (articles ?? []).slice(0, 3);

  return (
    <div className="py-12">
      {/* Terminal hero — a real API payload typed out, not decoration */}
      <section className="grid grid-cols-1 items-start gap-10 lg:grid-cols-[1fr_1.1fr]">
        <div className="min-w-0 pt-2">
          <p className="font-mono text-sm text-accent">$ whoami</p>
          <h1 className="mt-2 font-mono text-3xl font-semibold leading-tight sm:text-4xl">
            Mohammodullah Emran
          </h1>
          <p className="mt-1 font-mono text-lg text-text-dim">
            Software Engineer — (SRE)
          </p>
          <p className="mt-4 max-w-md leading-relaxed text-text-dim">
            I build observable, honestly-documented backend systems. The
            backend <span className="text-accent">is</span> the portfolio —
            this site is a thin client of a real, production-deployed .NET
            API, and every metric on it is real.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <a
              href="/resume.pdf"
              download="Mohammodullah_Emran_Resume.pdf"
              className="inline-flex items-center gap-2 rounded border border-transparent bg-accent px-4 py-2 font-mono text-sm font-semibold text-bg transition-colors hover:opacity-90"
            >
              resume ↓
            </a>
            <Button href="https://github.com/emranmho" variant="ghost">
              github ↗
            </Button>
            <Button
              href="https://www.linkedin.com/in/emranmho"
              variant="ghost"
            >
              linkedin ↗
            </Button>
          </div>
        </div>
        <div className="min-w-0">
          <TerminalWindow
            command="curl https://api.emran.blog/api/whoami"
            output={
              whoami
                ? JSON.stringify(whoami, null, 2)
                : `curl: (7) Failed to connect to api.emran.blog — API offline`
            }
          />
          {/* The site invites you to leave the browser. */}
          <div className="mt-3 flex items-center justify-between gap-3 border border-border bg-surface px-4 py-2.5">
            <code className="min-w-0 flex-1 truncate font-mono text-sm text-text-dim">
              curl https://api.emran.blog/api/whoami
            </code>
            <CopyButton text="curl https://api.emran.blog/api/whoami" />
          </div>
        </div>
      </section>

      {/* Live metric strip — fed by /api/metrics, honest numbers */}
      <section className="mt-16">
        <div className="mb-4 flex items-baseline gap-3">
          <h2 className="font-mono text-sm font-semibold text-text-dim">
            {"// live from /api/metrics"}
          </h2>
        </div>
        {metrics ? (
          <div className="grid grid-cols-2 gap-px border border-border bg-border sm:grid-cols-4 [&>*]:border-0">
            <MetricChip
              label="uptime"
              value={formatUptime(metrics.uptimeSeconds)}
              state="ok"
            />
            <MetricChip
              label="requests today"
              value={formatCount(metrics.requestsToday)}
            />
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
        ) : (
          <p className="border border-border bg-surface p-4 font-mono text-sm text-err">
            /api/metrics unreachable — the status dot in the nav is red for a
            reason.
          </p>
        )}
      </section>

      {/* Featured projects as endpoint cards */}
      {featured.length > 0 && (
        <section className="mt-16">
          <div className="mb-5 flex items-baseline justify-between">
            <h2 className="font-mono text-lg font-semibold">Featured</h2>
            <Link
              href="/projects"
              className="font-mono text-sm text-text-dim hover:text-accent"
            >
              GET /projects →
            </Link>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            {featured.map((p) => (
              <EndpointCard key={p.slug} project={p} />
            ))}
          </div>
        </section>
      )}

      {/* Latest notes — two-column list layout, not another card grid */}
      {latest.length > 0 && (
        <section className="mt-16">
          <div className="grid gap-8 md:grid-cols-[200px_1fr]">
            <div>
              <h2 className="font-mono text-lg font-semibold">Notes</h2>
              <p className="mt-2 text-sm leading-relaxed text-text-dim">
                War stories and architecture write-ups. Numbers over adjectives.
              </p>
              <Link
                href="/notes"
                className="mt-3 inline-block font-mono text-sm text-accent hover:underline"
              >
                all notes →
              </Link>
            </div>
            <div>
              {latest.map((a) => (
                <ArticleCard key={a.slug} article={a} />
              ))}
            </div>
          </div>
        </section>
      )}
    </div>
  );
}
