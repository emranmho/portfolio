using MediatR;
using Portfolio.Application.Abstractions;

namespace Portfolio.Application.Features.Articles.GetArticleBySlug;

public sealed class GetArticleBySlugHandler(IArticleReader articles)
    : IRequestHandler<GetArticleBySlugQuery, GetArticleBySlugResponse?>
{
    public async Task<GetArticleBySlugResponse?> Handle(GetArticleBySlugQuery request, CancellationToken ct)
    {
        var article = await articles.GetBySlugAsync(request.Slug, ct);
        if (article is null)
            return null;

        return new GetArticleBySlugResponse(
            article.Slug,
            article.Title,
            article.Summary,
            article.PublishedAtUtc,
            article.Tags,
            article.ReadingTimeMinutes,
            article.Html,
            article.RawMarkdown);
    }
}
