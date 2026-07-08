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
  const [typedCommand, setTypedCommand] = useState("");
  const [showOutput, setShowOutput] = useState(false);
  const reduced = useRef(false);

  useEffect(() => {
    reduced.current = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    if (reduced.current) {
      setTypedCommand(command);
      setShowOutput(true);
      return;
    }
    let i = 0;
    const timer = setInterval(() => {
      i += 1;
      setTypedCommand(command.slice(0, i));
      if (i >= command.length) {
        clearInterval(timer);
        setTimeout(() => setShowOutput(true), 250);
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
          {!showOutput && <span className="cursor-blink text-accent">▊</span>}
        </div>
        {showOutput && (
          <>
            <pre className="mt-2 whitespace-pre text-text-dim">{output}</pre>
            <div className="mt-2">
              <span className="text-accent">$ </span>
              <span className="cursor-blink text-accent">▊</span>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
