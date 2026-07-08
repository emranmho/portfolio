"use client";

import { useState } from "react";

type Json = string | number | boolean | null | Json[] | { [key: string]: Json };

// Collapsible JSON tree — built for the /playground (Phase 4), also used
// anywhere a raw API payload is shown.
export function JsonViewer({ data }: { data: Json }) {
  return (
    <div className="overflow-x-auto border border-border bg-surface p-4 font-mono text-sm leading-relaxed">
      <JsonNode value={data} depth={0} />
    </div>
  );
}

function JsonNode({ value, depth }: { value: Json; depth: number }) {
  const [open, setOpen] = useState(depth < 2);

  if (value === null) return <span className="text-text-dim">null</span>;
  if (typeof value === "string")
    return <span className="text-accent">&quot;{value}&quot;</span>;
  if (typeof value === "number") return <span className="text-warn">{value}</span>;
  if (typeof value === "boolean")
    return <span className="text-warn">{String(value)}</span>;

  const isArray = Array.isArray(value);
  const entries = isArray
    ? (value as Json[]).map((v, i) => [String(i), v] as const)
    : Object.entries(value);
  const [openBracket, closeBracket] = isArray ? ["[", "]"] : ["{", "}"];

  if (entries.length === 0)
    return <span>{openBracket + closeBracket}</span>;

  if (!open) {
    return (
      <button
        onClick={() => setOpen(true)}
        className="cursor-pointer text-text-dim hover:text-accent"
        aria-label="Expand"
      >
        {openBracket} … {closeBracket}
        <span className="ml-1 text-xs">({entries.length})</span>
      </button>
    );
  }

  return (
    <span>
      <button
        onClick={() => setOpen(false)}
        className="cursor-pointer hover:text-accent"
        aria-label="Collapse"
      >
        {openBracket}
      </button>
      <div className="ml-4 border-l border-border pl-3">
        {entries.map(([key, v], i) => (
          <div key={key}>
            {!isArray && <span className="text-text">&quot;{key}&quot;: </span>}
            <JsonNode value={v} depth={depth + 1} />
            {i < entries.length - 1 && <span className="text-text-dim">,</span>}
          </div>
        ))}
      </div>
      <span>{closeBracket}</span>
    </span>
  );
}
