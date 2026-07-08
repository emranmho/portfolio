namespace Portfolio.Application.Features.Articles.GetArticleBySlug;

public sealed record GetArticleBySlugResponse(
    string Slug,
    string Title,
    string Summary,
    DateTime PublishedAtUtc,
    IReadOnlyList<string> Tags,
    int ReadingTimeMinutes,
    string Html,
    string RawMarkdown);
