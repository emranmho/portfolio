import Link from "next/link";
import { Badge } from "./Badge";
import type { Project } from "@/lib/api";

// The project card, styled as an API endpoint — method badge, path,
// status, tech tags.
export function EndpointCard({ project }: { project: Project }) {
  return (
    <Link
      href={`/projects/${project.slug}`}
      className="group block border border-border bg-surface p-5 transition-colors hover:border-accent"
    >
      <div className="flex items-center gap-3 font-mono text-sm">
        <span className="rounded bg-accent/15 px-1.5 py-0.5 text-xs font-semibold text-accent">
          GET
        </span>
        <span className="truncate text-text group-hover:text-accent">
          /projects/{project.slug}
        </span>
        <span className="ml-auto shrink-0 text-xs text-accent">200 OK</span>
      </div>
      <h3 className="mt-3 font-mono font-semibold">{project.name}</h3>
      <p className="mt-1.5 text-sm leading-relaxed text-text-dim">
        {project.summary}
      </p>
      <div className="mt-4 flex flex-wrap gap-1.5">
        {project.stack.map((tech) => (
          <Badge key={tech}>{tech}</Badge>
        ))}
      </div>
    </Link>
  );
}
