import type { Metadata } from "next";
import Link from "next/link";
import { api } from "@/lib/api";
import { EndpointCard } from "@/components/EndpointCard";

export const metadata: Metadata = {
  title: "Projects",
  description: "Projects served as API endpoints — because they are.",
};

export default async function ProjectsPage({
  searchParams,
}: {
  searchParams: Promise<{ stack?: string }>;
}) {
  const { stack } = await searchParams;
  const [projects, all] = await Promise.all([
    api.projects(stack),
    stack ? api.projects() : Promise.resolve(null),
  ]);

  const stacks = [...new Set((all ?? projects ?? []).flatMap((p) => p.stack))];

  return (
    <div className="py-12">
      <div className="flex flex-wrap items-baseline gap-x-6 gap-y-2">
        <h1 className="font-mono text-2xl font-semibold">
          <span className="text-accent">GET</span> /api/projects
        </h1>
        {stack && (
          <span className="font-mono text-sm text-text-dim">?stack={stack}</span>
        )}
      </div>

      <div className="mt-6 flex flex-wrap gap-2 font-mono text-sm">
        <Link
          href="/projects"
          className={`rounded border px-3 py-1 transition-colors ${
            !stack
              ? "border-accent text-accent"
              : "border-border text-text-dim hover:border-accent hover:text-accent"
          }`}
        >
          all
        </Link>
        {stacks.map((s) => (
          <Link
            key={s}
            href={`/projects?stack=${s}`}
            className={`rounded border px-3 py-1 transition-colors ${
              stack === s
                ? "border-accent text-accent"
                : "border-border text-text-dim hover:border-accent hover:text-accent"
            }`}
          >
            {s}
          </Link>
        ))}
      </div>

      {projects && projects.length > 0 ? (
        <div className="mt-8 grid gap-4 md:grid-cols-2">
          {projects.map((p) => (
            <EndpointCard key={p.slug} project={p} />
          ))}
        </div>
      ) : (
        <p className="mt-8 border border-border bg-surface p-4 font-mono text-sm text-text-dim">
          {projects ? "[] — no projects match that filter" : "API unreachable"}
        </p>
      )}
    </div>
  );
}
