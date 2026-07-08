import { ImageResponse } from "next/og";
import { api } from "@/lib/api";

export const alt = "Article on emran.blog";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

// Per-article OG card: mono title on the terminal-dark background.
export default async function OpenGraphImage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  let title = slug;
  let tags: string[] = [];
  let readingTime = 0;
  try {
    const article = await api.article(slug);
    if (article) {
      title = article.title;
      tags = article.tags;
      readingTime = article.readingTimeMinutes;
    }
  } catch {
    // Unknown slug or API down — fall back to the slug as title.
  }

  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          backgroundColor: "#0B0E0D",
          padding: 64,
          fontFamily: "monospace",
        }}
      >
        <div style={{ display: "flex", fontSize: 30, color: "#4ADE80" }}>
          $ cat notes/{slug}.md
        </div>
        <div
          style={{
            display: "flex",
            marginTop: 40,
            fontSize: title.length > 40 ? 56 : 68,
            fontWeight: 700,
            color: "#E6EAE8",
            lineHeight: 1.15,
          }}
        >
          {title}
        </div>
        <div style={{ display: "flex", gap: 16, marginTop: 32 }}>
          {tags.map((t) => (
            <div
              key={t}
              style={{
                display: "flex",
                border: "1px solid #222826",
                borderRadius: 6,
                padding: "6px 14px",
                color: "#8A938F",
                fontSize: 24,
              }}
            >
              {t}
            </div>
          ))}
          {readingTime > 0 && (
            <div style={{ display: "flex", color: "#8A938F", fontSize: 24, padding: "6px 0" }}>
              {readingTime} min read
            </div>
          )}
        </div>
        <div
          style={{
            display: "flex",
            marginTop: "auto",
            justifyContent: "space-between",
            fontSize: 28,
          }}
        >
          <div style={{ display: "flex", color: "#4ADE80" }}>emran.blog ▊</div>
          <div style={{ display: "flex", color: "#8A938F" }}>
            served by a real .NET API
          </div>
        </div>
      </div>
    ),
    size,
  );
}
