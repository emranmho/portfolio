import Link from "next/link";
import { Badge } from "./Badge";
import { formatDate } from "@/lib/format";
import type { ArticleSummary } from "@/lib/api";

export function ArticleCard({ article }: { article: ArticleSummary }) {
  return (
    <Link
      href={`/notes/${article.slug}`}
      className="group block border-b border-border py-5 transition-colors"
    >
      <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
        <h3 className="font-mono font-semibold group-hover:text-accent">
          {article.title}
        </h3>
        <span className="font-mono text-xs text-text-dim">
          {formatDate(article.publishedAtUtc)} · {article.readingTimeMinutes} min
        </span>
      </div>
      <p className="mt-1.5 max-w-2xl text-sm leading-relaxed text-text-dim">
        {article.summary}
      </p>
      <div className="mt-2.5 flex gap-1.5">
        {article.tags.map((tag) => (
          <Badge key={tag}>{tag}</Badge>
        ))}
      </div>
    </Link>
  );
}
