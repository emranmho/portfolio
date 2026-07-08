import type { Metadata } from "next";
import { PlaygroundClient } from "./playground-client";

export const metadata: Metadata = {
  title: "Playground",
  description:
    "Operate the API from the browser: live responses, real rate-limit headers, and a three-click JWT demo.",
};

export default function PlaygroundPage() {
  return (
    <div className="py-12">
      <header className="max-w-2xl">
        <p className="font-mono text-sm text-accent">$ ./playground</p>
        <h1 className="mt-2 font-mono text-3xl font-semibold">Playground</h1>
        <p className="mt-3 leading-relaxed text-text-dim">
          Every request below hits the real production API from your browser —
          same rate limiter, same JWT validation, same metrics middleware that
          feeds <span className="font-mono">/status</span>. Nothing is mocked.
        </p>
      </header>
      <PlaygroundClient />
    </div>
  );
}
