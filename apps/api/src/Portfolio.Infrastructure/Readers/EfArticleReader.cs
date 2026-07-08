using Microsoft.EntityFrameworkCore;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Readers;

public sealed class EfArticleReader(PortfolioDbContext db) : IArticleReader
{
    public async Task<IReadOnlyList<ArticleSummaryDto>> GetAllAsync(CancellationToken ct)
    {
        var articles = await db.Articles.AsNoTracking()
            .OrderByDescending(a => a.PublishedAtUtc)
            .ToListAsync(ct);

        return articles
            .Select(a => new ArticleSummaryDto(
                a.Slug, a.Title, a.Summary, a.PublishedAtUtc, a.Tags, a.ReadingTimeMinutes))
            .ToList();
    }

    public async Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var a = await db.Articles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLowerInvariant(), ct);

        return a is null
            ? null
            : new ArticleDetailDto(
                a.Slug, a.Title, a.Summary, a.PublishedAtUtc, a.Tags,
                a.ReadingTimeMinutes, a.Html, a.RawMarkdown);
    }
}
