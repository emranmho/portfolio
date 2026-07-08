import { api } from "@/lib/api";

const SITE = "https://emran.blog";

function esc(s: string): string {
  return s
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

// RSS 2.0 over the same /api/articles the site renders from. The api client
// fetches with ISR revalidation, so this stays fresh without being dynamic.
export async function GET() {
  const articles = (await api.articles()) ?? [];

  const items = articles
    .map(
      (a) => `    <item>
      <title>${esc(a.title)}</title>
      <link>${SITE}/notes/${a.slug}</link>
      <guid isPermaLink="true">${SITE}/notes/${a.slug}</guid>
      <description>${esc(a.summary)}</description>
      <pubDate>${new Date(a.publishedAtUtc).toUTCString()}</pubDate>
      ${a.tags.map((t) => `<category>${esc(t)}</category>`).join("\n      ")}
    </item>`,
    )
    .join("\n");

  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    <title>emran.blog — notes</title>
    <link>${SITE}/notes</link>
    <description>War stories and architecture write-ups. Numbers over adjectives.</description>
    <language>en</language>
    <atom:link href="${SITE}/feed.xml" rel="self" type="application/rss+xml" />
${items}
  </channel>
</rss>`;

  return new Response(xml, {
    headers: {
      "Content-Type": "application/rss+xml; charset=utf-8",
    },
  });
}
