type State = "ok" | "warn" | "err" | "neutral";

const stateColor: Record<State, string> = {
  ok: "text-accent",
  warn: "text-warn",
  err: "text-err",
  neutral: "text-text",
};

export function MetricChip({
  label,
  value,
  state = "neutral",
}: {
  label: string;
  value: string;
  state?: State;
}) {
  return (
    <div className="flex flex-col gap-1 border border-border bg-surface px-4 py-3">
      <span className="font-mono text-xs text-text-dim">{label}</span>
      <span className={`font-mono text-lg font-semibold ${stateColor[state]}`}>
        {value}
      </span>
    </div>
  );
}
