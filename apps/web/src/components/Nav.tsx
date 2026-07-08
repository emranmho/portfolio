import Link from "next/link";
import { api } from "@/lib/api";

const links = [
  { href: "/projects", label: "projects" },
  { href: "/playground", label: "playground" },
  { href: "/status", label: "status" },
  { href: "/notes", label: "notes" },
  { href: "/about", label: "about" },
];

// Server component — the status dot is fed by a real /api/health call
// (ISR, 30s), same data the Docker healthcheck sees.
export async function Nav() {
  const health = await api.health();
  const up = health?.status === "ok";

  return (
    <header className="border-b border-border">
      <nav className="mx-auto flex max-w-[1100px] items-center gap-6 px-6 py-4 font-mono text-sm">
        <Link href="/" className="font-semibold text-text hover:text-accent">
          emran<span className="text-accent">.blog</span>
        </Link>
        <div className="ml-auto flex items-center gap-5">
          {links.map((l) => (
            <Link
              key={l.href}
              href={l.href}
              className="text-text-dim transition-colors hover:text-accent"
            >
              {l.label}
            </Link>
          ))}
          <a
            href={`${api.baseUrl}/docs`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-text-dim transition-colors hover:text-accent"
          >
            /docs
          </a>
          <span
            className="flex items-center gap-1.5 text-xs"
            title={up ? `API up · ${health?.gitSha}` : "API unreachable"}
          >
            <span
              className={`h-2 w-2 rounded-full ${up ? "bg-accent" : "bg-err"}`}
            />
            <span className={up ? "text-accent" : "text-err"}>
              {up ? "up" : "down"}
            </span>
          </span>
        </div>
      </nav>
    </header>
  );
}
