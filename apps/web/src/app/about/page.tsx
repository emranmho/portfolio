import type { Metadata } from "next";
import Link from "next/link";
import { api } from "@/lib/api";
import { Badge } from "@/components/Badge";

export const metadata: Metadata = {
  title: "About",
  description: "Backend engineer. The site is the proof, not the claim.",
};

// ── EDIT ME: real work history goes here ─────────────────────────────
// Placeholder entries below are drawn from the README's war-story hints.
// Replace company/period/points with the facts from your resume.
const experience = [
  {
    role: "Backend Engineer (.NET)",
    company: "TODO: Company name",
    period: "TODO: 2024 — present",
    stack: ["dotnet", "csharp", "sqlserver", "efcore"],
    points: [
      "TODO: Cut SQL Server query time 40% — real query plans, before/after (see the war-story article).",
      "TODO: One more measurable outcome. Numbers over adjectives.",
    ],
  },
  {
    role: "Software Engineer",
    company: "TODO: Previous company",
    period: "TODO: 2021 — 2024",
    stack: ["dotnet", "aspnet"],
    points: [
      "TODO: Migrated .NET Framework → Core with 99.9% uptime — strategy, rollback plan, what broke.",
    ],
  },
];

const contact = [
  { label: "email", value: "emran2300@gmail.com", href: "mailto:emran2300@gmail.com" },
  { label: "github", value: "github.com/emranmho", href: "https://github.com/emranmho" },
  { label: "linkedin", value: "linkedin.com/in/emranmho", href: "https://www.linkedin.com/in/emranmho" },
  { label: "location", value: "Bangladesh", href: null },
];

export default function AboutPage() {
  return (
    <div className="max-w-2xl py-12">
      <h1 className="font-mono text-2xl font-semibold">
        <span className="text-accent">$</span> whoami
      </h1>

      <section className="mt-8">
        <p className="leading-relaxed">
          I&apos;m Emran — a backend engineer working primarily in{" "}
          <span className="font-mono text-accent">.NET / C#</span>, with a
          growing Go habit. I care about systems that can be observed, deployed
          safely, and explained honestly.
        </p>
        <p className="mt-4 leading-relaxed text-text-dim">
          Day to day that means APIs, data access that respects the database,
          CI/CD pipelines that gate on tests, and knowing which tools{" "}
          <em>not</em> to reach for. This site practices what it preaches: it is
          a thin client of a real, production-deployed .NET API, and every
          number on it is real.
        </p>
        <div className="mt-6">
          <a
            href="/resume.pdf"
            download="Mohammodullah_Emran_Resume.pdf"
            className="inline-flex items-center gap-2 rounded border border-transparent bg-accent px-4 py-2 font-mono text-sm font-semibold text-bg transition-colors hover:opacity-90"
          >
            resume ↓
          </a>
        </div>
      </section>

      <section className="mt-10">
        <h2 className="font-mono text-lg font-semibold">
          <span className="text-text-dim">## </span>Experience
        </h2>
        <div className="mt-5 space-y-8 border-l border-border pl-5">
          {experience.map((job) => (
            <div key={`${job.company}-${job.period}`} className="relative">
              <span className="absolute -left-[26px] top-1.5 h-2 w-2 rounded-full bg-accent" />
              <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
                <h3 className="font-mono font-semibold">{job.role}</h3>
                <span className="font-mono text-xs text-text-dim">
                  {job.company} · {job.period}
                </span>
              </div>
              <ul className="mt-2 space-y-1.5 text-sm leading-relaxed text-text-dim">
                {job.points.map((point) => (
                  <li key={point}>
                    <span className="text-accent">— </span>
                    {point}
                  </li>
                ))}
              </ul>
              <div className="mt-2.5 flex flex-wrap gap-1.5">
                {job.stack.map((tech) => (
                  <Badge key={tech}>{tech}</Badge>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="mt-10">
        <h2 className="font-mono text-lg font-semibold">
          <span className="text-text-dim">## </span>Philosophy
        </h2>
        <ul className="mt-4 space-y-2 font-mono text-sm leading-relaxed">
          <li>
            <span className="text-accent">→</span> Prove, don&apos;t claim.
          </li>
          <li>
            <span className="text-accent">→</span> Proportionate architecture —
            knowing when <em>not</em> to use a tool is the senior signal.
          </li>
          <li>
            <span className="text-accent">→</span> Never half-built. Deployed
            beats planned.
          </li>
          <li>
            <span className="text-accent">→</span> Honest beats impressive.
          </li>
        </ul>
      </section>

      <section className="mt-10">
        <h2 className="font-mono text-lg font-semibold">
          <span className="text-text-dim">## </span>How this site works
        </h2>
        <p className="mt-3 leading-relaxed text-text-dim">
          Every byte here is served by a .NET 10 API — content ingested from
          git, metrics collected by custom middleware into SQLite, deployed by
          Dokploy behind Traefik with tests gating every deploy. The full
          write-up:{" "}
          <Link
            href="/notes/how-this-portfolio-works"
            className="text-accent underline underline-offset-4"
          >
            How this portfolio works
          </Link>
          .
        </p>
        <p className="mt-4 border border-border bg-surface p-3 font-mono text-sm text-text-dim">
          <span className="text-accent">$</span> curl {api.baseUrl}/api/whoami
        </p>
      </section>

      <section className="mt-10">
        <h2 className="font-mono text-lg font-semibold">
          <span className="text-text-dim">## </span>Contact
        </h2>
        <dl className="mt-4 space-y-2 font-mono text-sm">
          {contact.map((c) => (
            <div key={c.label} className="flex gap-3">
              <dt className="w-24 text-text-dim">{c.label}</dt>
              <dd>
                {c.href ? (
                  <a
                    href={c.href}
                    {...(c.href.startsWith("http")
                      ? { target: "_blank", rel: "noopener noreferrer" }
                      : {})}
                    className="text-accent hover:underline"
                  >
                    {c.value}
                  </a>
                ) : (
                  c.value
                )}
              </dd>
            </div>
          ))}
          <div className="flex gap-3">
            <dt className="w-24 text-text-dim">api docs</dt>
            <dd>
              <a
                href={`${api.baseUrl}/docs`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-accent hover:underline"
              >
                {api.baseUrl.replace(/^https?:\/\//, "")}/docs
              </a>
            </dd>
          </div>
        </dl>
      </section>
    </div>
  );
}
