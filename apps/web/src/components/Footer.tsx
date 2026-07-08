import { api } from "@/lib/api";

export function Footer() {
  return (
    <footer className="mt-16 border-t border-border">
      <div className="mx-auto flex max-w-[1100px] flex-wrap items-center gap-x-6 gap-y-2 px-6 py-8 font-mono text-xs text-text-dim">
        <span>© {new Date().getFullYear()} Emran</span>
        <span>
          built with <span className="text-text">.NET 10</span> +{" "}
          <span className="text-text">Next.js</span>
        </span>
        <div className="ml-auto flex gap-5">
          <a href="/feed.xml" className="hover:text-accent">
            rss
          </a>
          <a
            href="https://github.com/emranmho/portfolio"
            target="_blank"
            rel="noopener noreferrer"
            className="hover:text-accent"
          >
            source
          </a>
          <a
            href={`${api.baseUrl}/docs`}
            target="_blank"
            rel="noopener noreferrer"
            className="hover:text-accent"
          >
            api docs
          </a>
          <a
            href={`${api.baseUrl}/api/whoami`}
            target="_blank"
            rel="noopener noreferrer"
            className="hover:text-accent"
          >
            /api/whoami
          </a>
        </div>
      </div>
    </footer>
  );
}
