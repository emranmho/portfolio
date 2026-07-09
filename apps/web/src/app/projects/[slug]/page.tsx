import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { api, ApiError, type Project } from "@/lib/api";
import { Badge } from "@/components/Badge";
import { Button } from "@/components/Button";
import { highlightArticleHtml } from "@/lib/highlight";

async function getProject(slug: string): Promise<Project | null> {
  try {
    return await api.project(slug);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    throw err;
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const project = await getProject(slug);
  return {
    title: project?.name ?? slug,
    description: project?.summary,
  };
}

export default async function ProjectDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const project = await getProject(slug);

  if (!project) {
    return (
      <p className="my-12 border border-border bg-surface p-4 font-mono text-sm text-err">
        API unreachable — try again shortly.
      </p>
    );
  }

  const descriptionHtml = await highlightArticleHtml(project.descriptionHtml);

  return (
    <article className="py-12">
      <Link
        href="/projects"
        className="font-mono text-sm text-text-dim hover:text-accent"
      >
        ← /api/projects
      </Link>

      <div className="mt-6 flex flex-wrap items-center gap-3 font-mono text-sm">
        <span className="rounded bg-accent/15 px-1.5 py-0.5 text-xs font-semibold text-accent">
          GET
        </span>
        <span className="text-text-dim">/api/projects/{project.slug}</span>
        <span className="text-xs text-accent">200 OK</span>
      </div>

      <h1 className="mt-4 font-mono text-3xl font-semibold">{project.name}</h1>
      <p className="mt-3 max-w-2xl text-lg leading-relaxed text-text-dim">
        {project.summary}
      </p>

      <div className="mt-5 flex flex-wrap gap-1.5">
        {project.stack.map((tech) => (
          <Badge key={tech} tone="accent">
            {tech}
          </Badge>
        ))}
      </div>

      <section className="mt-10 max-w-2xl">
        <h2 className="font-mono text-lg font-semibold">
          <span className="text-text-dim">## </span>What & why
        </h2>
        <div
          className="prose mt-3 max-w-none"
          dangerouslySetInnerHTML={{ __html: descriptionHtml }}
        />
      </section>

      <div className="mt-10 flex flex-wrap gap-3">
        {project.liveUrl && <Button href={project.liveUrl}>view live</Button>}
        {project.repoUrl && (
          <Button href={project.repoUrl} variant="ghost">
            source on GitHub
          </Button>
        )}
      </div>
    </article>
  );
}
