"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { PUBLIC_API_BASE_URL } from "@/lib/api";

interface Command {
  id: string;
  label: string;
  hint: string;
  href: string;
  external?: boolean;
}

const commands: Command[] = [
  { id: "home", label: "home", hint: "/", href: "/" },
  { id: "projects", label: "projects", hint: "/projects", href: "/projects" },
  {
    id: "playground",
    label: "playground",
    hint: "/playground",
    href: "/playground",
  },
  { id: "status", label: "status", hint: "/status", href: "/status" },
  { id: "notes", label: "notes", hint: "/notes", href: "/notes" },
  { id: "about", label: "about", hint: "/about", href: "/about" },
  {
    id: "docs",
    label: "api docs",
    hint: "↗ scalar",
    href: `${PUBLIC_API_BASE_URL}/docs`,
    external: true,
  },
  {
    id: "resume",
    label: "resume",
    hint: "↗ json",
    href: `${PUBLIC_API_BASE_URL}/api/resume`,
    external: true,
  },
];

// Global Cmd+K / Ctrl+K launcher. Terminal-chrome styled to match TerminalWindow —
// this is navigation, not a search bar with a keyboard shortcut bolted on.
export function CommandPalette() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const router = useRouter();

  const filtered = commands.filter((c) =>
    c.label.toLowerCase().includes(query.trim().toLowerCase()),
  );

  const close = useCallback(() => {
    setOpen(false);
    setQuery("");
    setSelected(0);
  }, []);

  const run = useCallback(
    (command: Command | undefined) => {
      if (!command) return;
      if (command.external) {
        window.open(command.href, "_blank", "noopener,noreferrer");
      } else {
        router.push(command.href);
      }
      close();
    },
    [close, router],
  );

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setOpen((v) => !v);
        return;
      }
      if (!open) return;
      if (e.key === "Escape") {
        e.preventDefault();
        close();
      } else if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelected((i) => (i + 1) % Math.max(filtered.length, 1));
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelected(
          (i) => (i - 1 + Math.max(filtered.length, 1)) % Math.max(filtered.length, 1),
        );
      } else if (e.key === "Enter") {
        e.preventDefault();
        run(filtered[selected]);
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, filtered, selected, close, run]);

  useEffect(() => {
    function onTrigger() {
      setOpen(true);
    }
    window.addEventListener("open-command-palette", onTrigger);
    return () => window.removeEventListener("open-command-palette", onTrigger);
  }, []);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  function onQueryChange(value: string) {
    setQuery(value);
    setSelected(0);
  }

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-bg/70 pt-[15vh] backdrop-blur-sm"
      onClick={close}
    >
      <div
        className="w-full max-w-[560px] overflow-hidden rounded-lg border border-border bg-surface shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-2 border-b border-border px-4 py-2.5">
          <span className="h-3 w-3 rounded-full bg-err/70" />
          <span className="h-3 w-3 rounded-full bg-warn/70" />
          <span className="h-3 w-3 rounded-full bg-accent/70" />
          <span className="ml-2 font-mono text-xs text-text-dim">
            cmdk — emran.blog
          </span>
        </div>
        <div className="flex items-center gap-2 border-b border-border px-4 py-3 font-mono text-sm">
          <span className="text-accent">$</span>
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => onQueryChange(e.target.value)}
            placeholder="jump to…"
            className="w-full bg-transparent text-text outline-none placeholder:text-text-dim"
          />
        </div>
        <div className="max-h-[320px] overflow-y-auto p-1.5 font-mono text-sm">
          {filtered.length === 0 && (
            <p className="px-3 py-4 text-text-dim">no matches</p>
          )}
          {filtered.map((c, i) => (
            <button
              key={c.id}
              onClick={() => run(c)}
              onMouseEnter={() => setSelected(i)}
              className={`flex w-full items-center justify-between rounded px-3 py-2 text-left transition-colors ${
                i === selected
                  ? "bg-bg text-accent"
                  : "text-text hover:bg-bg"
              }`}
            >
              <span>
                <span className="text-text-dim">→ </span>
                {c.label}
              </span>
              <span className="text-xs text-text-dim">{c.hint}</span>
            </button>
          ))}
        </div>
        <div className="border-t border-border px-4 py-2 font-mono text-xs text-text-dim">
          ↑↓ navigate · ↵ select · esc close
        </div>
      </div>
    </div>
  );
}
