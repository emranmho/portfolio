"use client";

import { useEffect, useRef, useState } from "react";

// Terminal chrome + typed-out effect. The typed content is a real API
// payload passed in from a server component — the terminal is a live
// API call, not decoration.
export function TerminalWindow({
  command,
  output,
  title = "bash — emran.blog",
}: {
  command: string;
  output: string;
  title?: string;
}) {
  // The output is real data, already fetched server-side — it renders
  // immediately so it counts as real content (LCP, crawlers, no fake
  // loading state). Only the command echo above it is cosmetically typed.
  const [typedCommand, setTypedCommand] = useState(command);
  const [typing, setTyping] = useState(false);
  const reduced = useRef(false);

  useEffect(() => {
    reduced.current = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    if (reduced.current) return;

    setTypedCommand("");
    setTyping(true);
    let i = 0;
    const timer = setInterval(() => {
      i += 1;
      setTypedCommand(command.slice(0, i));
      if (i >= command.length) {
        clearInterval(timer);
        setTyping(false);
      }
    }, 35);
    return () => clearInterval(timer);
  }, [command]);

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-surface">
      <div className="flex items-center gap-2 border-b border-border px-4 py-2.5">
        <span className="h-3 w-3 rounded-full bg-err/70" />
        <span className="h-3 w-3 rounded-full bg-warn/70" />
        <span className="h-3 w-3 rounded-full bg-accent/70" />
        <span className="ml-2 font-mono text-xs text-text-dim">{title}</span>
      </div>
      <div className="overflow-x-auto p-4 font-mono text-sm leading-relaxed">
        <div>
          <span className="text-accent">$ </span>
          <span>{typedCommand}</span>
          {typing && <span className="cursor-blink text-accent">▊</span>}
        </div>
        <pre className="mt-2 whitespace-pre text-text-dim">{output}</pre>
        <div className="mt-2">
          <span className="text-accent">$ </span>
          {!typing && <span className="cursor-blink text-accent">▊</span>}
        </div>
      </div>
    </div>
  );
}
