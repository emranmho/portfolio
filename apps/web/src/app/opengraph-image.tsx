import { ImageResponse } from "next/og";

export const alt = "emran.blog — the backend is the portfolio";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

// Terminal-styled OG card matching the site's design tokens. Satori can't
// load next/font, so this leans on layout + color rather than the exact mono.
export default function OpenGraphImage() {
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
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            flex: 1,
            border: "1px solid #222826",
            borderRadius: 12,
            backgroundColor: "#111514",
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "20px 28px",
              borderBottom: "1px solid #222826",
            }}
          >
            <div style={{ width: 16, height: 16, borderRadius: 8, backgroundColor: "#F87171", opacity: 0.7 }} />
            <div style={{ width: 16, height: 16, borderRadius: 8, backgroundColor: "#FBBF24", opacity: 0.7 }} />
            <div style={{ width: 16, height: 16, borderRadius: 8, backgroundColor: "#4ADE80", opacity: 0.7 }} />
            <div style={{ marginLeft: 12, color: "#8A938F", fontSize: 24 }}>
              bash — emran.blog
            </div>
          </div>
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              padding: "40px 48px",
              flex: 1,
            }}
          >
            <div style={{ display: "flex", fontSize: 34, color: "#4ADE80" }}>
              $ curl https://api.emran.blog/api/whoami
            </div>
            <div style={{ display: "flex", marginTop: 36, fontSize: 64, fontWeight: 700, color: "#E6EAE8" }}>
              Mohammodullah Emran
            </div>
            <div style={{ display: "flex", marginTop: 16, fontSize: 36, color: "#8A938F" }}>
              Software Engineer — the backend is the portfolio
            </div>
            <div style={{ display: "flex", marginTop: "auto", fontSize: 28, color: "#4ADE80" }}>
              emran.blog ▊
            </div>
          </div>
        </div>
      </div>
    ),
    size,
  );
}
