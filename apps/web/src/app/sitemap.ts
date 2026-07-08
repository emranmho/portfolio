import type { MetadataRoute } from "next";
import { api } from "@/lib/api";

const SITE = "https://emran.blog";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [articles, projects] = await Promise.all([
    api.articles(),
    api.projects(),
  ]);

  const staticRoutes: MetadataRoute.Sitemap = [
    { url: SITE, changeFrequency: "weekly", priority: 1 },
    { url: `${SITE}/projects`, changeFrequency: "monthly", priority: 0.8 },
    { url: `${SITE}/notes`, changeFrequency: "weekly", priority: 0.8 },
    { url: `${SITE}/playground`, changeFrequency: "monthly", priority: 0.7 },
    { url: `${SITE}/status`, changeFrequency: "monthly", priority: 0.5 },
    { url: `${SITE}/about`, changeFrequency: "monthly", priority: 0.6 },
  ];

  const articleRoutes: MetadataRoute.Sitemap = (articles ?? []).map((a) => ({
    url: `${SITE}/notes/${a.slug}`,
    lastModified: new Date(a.publishedAtUtc),
    changeFrequency: "yearly",
    priority: 0.7,
  }));

  const projectRoutes: MetadataRoute.Sitemap = (projects ?? []).map((p) => ({
    url: `${SITE}/projects/${p.slug}`,
    changeFrequency: "monthly",
    priority: 0.6,
  }));

  return [...staticRoutes, ...articleRoutes, ...projectRoutes];
}
