import { codeToHtml } from "shiki";

// Markdig (API-side) renders fenced blocks as
// <pre><code class="language-xxx">…escaped…</code></pre>.
// Re-render those with Shiki on the server — zero client JS.

const CODE_BLOCK =
  /<pre><code class="language-([\w#+-]+)">([\s\S]*?)<\/code><\/pre>/g;

function unescapeHtml(s: string): string {
  return s
    .replaceAll("&lt;", "<")
    .replaceAll("&gt;", ">")
    .replaceAll("&quot;", '"')
    .replaceAll("&#39;", "'")
    .replaceAll("&amp;", "&");
}

export async function highlightCode(code: string, lang: string): Promise<string> {
  try {
    return await codeToHtml(code, { lang, theme: "vitesse-dark" });
  } catch {
    return await codeToHtml(code, { lang: "text", theme: "vitesse-dark" });
  }
}

export async function highlightArticleHtml(html: string): Promise<string> {
  const matches = [...html.matchAll(CODE_BLOCK)];
  let out = html;
  for (const m of matches) {
    const [block, lang, escaped] = m;
    const highlighted = await highlightCode(unescapeHtml(escaped).trimEnd(), lang);
    out = out.replace(block, highlighted);
  }
  return out;
}
