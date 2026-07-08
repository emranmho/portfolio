import type { Metadata } from "next";
import { Inter, JetBrains_Mono } from "next/font/google";
import { Nav } from "@/components/Nav";
import { Footer } from "@/components/Footer";
import { CommandPalette } from "@/components/CommandPalette";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  weight: ["400", "600"],
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-jetbrains-mono",
  weight: ["400", "600"],
});

export const metadata: Metadata = {
  metadataBase: new URL("https://emran.blog"),
  title: {
    default: "Emran — Backend Engineer (.NET)",
    template: "%s · emran.blog",
  },
  description:
    "The backend is the portfolio. A thin client of a real, observable, production-deployed .NET API.",
  alternates: {
    types: {
      "application/rss+xml": "/feed.xml",
    },
  },
  openGraph: {
    siteName: "emran.blog",
    type: "website",
    url: "https://emran.blog",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${inter.variable} ${jetbrainsMono.variable} flex min-h-screen flex-col antialiased`}
      >
        <Nav />
        <main className="mx-auto w-full max-w-[1100px] flex-1 px-6">
          {children}
        </main>
        <Footer />
        <CommandPalette />
      </body>
    </html>
  );
}
