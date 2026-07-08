"use client";

import { useRef, useState } from "react";

// Tiny copy-to-clipboard affordance used by the hero curl snippet and the
// playground's copyable curl commands.
export function CopyButton({
  text,
  label = "copy",
}: {
  text: string;
  label?: string;
}) {
  const [copied, setCopied] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  async function copy() {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard API unavailable (http, permissions) — nothing to do.
    }
  }

  return (
    <button
      onClick={copy}
      aria-label={`Copy: ${text}`}
      className="cursor-pointer rounded border border-border px-2 py-0.5 font-mono text-xs text-text-dim transition-colors hover:border-accent hover:text-accent"
    >
      {copied ? "copied ✓" : label}
    </button>
  );
}
