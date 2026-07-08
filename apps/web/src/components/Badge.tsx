export function Badge({
  children,
  tone = "dim",
}: {
  children: React.ReactNode;
  tone?: "dim" | "accent" | "warn" | "err";
}) {
  const tones = {
    dim: "text-text-dim border-border",
    accent: "text-accent border-accent/40",
    warn: "text-warn border-warn/40",
    err: "text-err border-err/40",
  };
  return (
    <span
      className={`inline-block rounded border px-1.5 py-0.5 font-mono text-xs ${tones[tone]}`}
    >
      {children}
    </span>
  );
}
