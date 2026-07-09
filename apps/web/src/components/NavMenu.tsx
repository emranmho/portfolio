"use client";

import { useState } from "react";
import Link from "next/link";
import { CommandPaletteTrigger } from "@/components/CommandPaletteTrigger";

type NavLink = { href: string; label: string };

// Client island so Nav itself can stay a server component (live /api/health call).
// Below md: links collapse behind a hamburger dropdown. md and up: plain inline row.
export function NavMenu({ links, docsUrl }: { links: NavLink[]; docsUrl: string }) {
  const [open, setOpen] = useState(false);

  return (
    <div className="relative flex items-center">
      <div
        className={`${open ? "flex" : "hidden"} absolute right-0 top-full z-10 flex-col items-end gap-4 rounded border border-border bg-surface p-4 md:static md:z-auto md:flex md:flex-row md:items-center md:gap-5 md:border-0 md:bg-transparent md:p-0`}
      >
        {links.map((l) => (
          <Link
            key={l.href}
            href={l.href}
            onClick={() => setOpen(false)}
            className="text-text-dim transition-colors hover:text-accent"
          >
            {l.label}
          </Link>
        ))}
        <a
          href={docsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-text-dim transition-colors hover:text-accent"
        >
          /docs
        </a>
        <CommandPaletteTrigger />
      </div>
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label="Toggle menu"
        aria-expanded={open}
        className="text-text-dim hover:text-accent md:hidden"
      >
        {open ? "✕" : "☰"}
      </button>
    </div>
  );
}
