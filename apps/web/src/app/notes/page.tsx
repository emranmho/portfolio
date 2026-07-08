import type { Metadata } from "next";
import { api } from "@/lib/api";
import { ArticleCard } from "@/components/ArticleCard";

export const metadata: Metadata = {
  title: "Notes",
  description:
    "Architecture write-ups and war stories, published via git push.",
};

export default async function NotesPage() {
  const articles = await api.articles();

  return (
    <div className="py-12">
      <h1 className="font-mono text-2xl font-semibold">
        <span className="text-accent">GET</span> /api/articles
      </h1>
      <p className="mt-3 max-w-xl leading-relaxed text-text-dim">
        Every note is a markdown file in the repo — publishing is{" "}
        <code className="font-mono text-sm text-text">git push</code>. No CMS.
      </p>

      {articles && articles.length > 0 ? (
        <div className="mt-8 max-w-3xl">
          {articles.map((a) => (
            <ArticleCard key={a.slug} article={a} />
          ))}
        </div>
      ) : (
        <p className="mt-8 border border-border bg-surface p-4 font-mono text-sm text-text-dim">
          {articles ? "[] — nothing published yet" : "API unreachable"}
        </p>
      )}
    </div>
  );
}
