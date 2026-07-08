namespace Portfolio.Application.Abstractions;

public interface IArticleReader
{
    /// <summary>Metadata only, sorted by publish date descending.</summary>
    Task<IReadOnlyList<ArticleSummaryDto>> GetAllAsync(CancellationToken ct);

    Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken ct);
}
