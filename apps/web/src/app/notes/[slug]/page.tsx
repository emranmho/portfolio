import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { api, ApiError, type ArticleDetail } from "@/lib/api";
import { Badge } from "@/components/Badge";
import { formatDate } from "@/lib/format";
import { highlightArticleHtml } from "@/lib/highlight";

async function getArticle(slug: string): Promise<ArticleDetail | null> {
  try {
    return await api.article(slug);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    throw err;
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const article = await getArticle(slug);
  return {
    title: article?.title ?? slug,
    description: article?.summary,
  };
}

export default async function NotePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const article = await getArticle(slug);

  if (!article) {
    return (
      <p className="my-12 border border-border bg-surface p-4 font-mono text-sm text-err">
        API unreachable — try again shortly.
      </p>
    );
  }

  const html = await highlightArticleHtml(article.html);

  return (
    <article className="py-12">
      <Link
        href="/notes"
        className="font-mono text-sm text-text-dim hover:text-accent"
      >
        ← /api/articles
      </Link>

      <header className="mt-6 max-w-2xl">
        <h1 className="font-mono text-3xl font-semibold leading-tight">
          {article.title}
        </h1>
        <div className="mt-4 flex flex-wrap items-center gap-x-4 gap-y-2 font-mono text-xs text-text-dim">
          <span>{formatDate(article.publishedAtUtc)}</span>
          <span>{article.readingTimeMinutes} min read</span>
          <span className="flex gap-1.5">
            {article.tags.map((tag) => (
              <Badge key={tag}>{tag}</Badge>
            ))}
          </span>
        </div>
      </header>

      <div
        className="prose mt-8 max-w-2xl"
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </article>
  );
}
