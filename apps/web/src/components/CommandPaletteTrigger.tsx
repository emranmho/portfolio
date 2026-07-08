"use client";

// Nav is a server component (it awaits a live /api/health call), so the
// clickable ⌘K hint lives in its own client island and just dispatches the
// event CommandPalette is already listening for.
export function CommandPaletteTrigger() {
  return (
    <button
      onClick={() => window.dispatchEvent(new CustomEvent("open-command-palette"))}
      className="flex items-center gap-1 rounded border border-border px-1.5 py-0.5 text-xs text-text-dim transition-colors hover:border-accent hover:text-accent"
      title="Command palette"
    >
      ⌘K
    </button>
  );
}
