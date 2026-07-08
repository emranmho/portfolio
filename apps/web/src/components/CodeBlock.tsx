import { highlightCode } from "@/lib/highlight";

// Server component — Shiki runs at render time, zero client JS ships.
export async function CodeBlock({
  code,
  lang = "text",
}: {
  code: string;
  lang?: string;
}) {
  const html = await highlightCode(code.trimEnd(), lang);
  return (
    <div
      className="prose overflow-x-auto"
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
