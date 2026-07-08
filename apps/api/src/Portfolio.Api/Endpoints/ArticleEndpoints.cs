using MediatR;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Features.Articles.GetArticleBySlug;

namespace Portfolio.Api.Endpoints;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles").WithTags("Content");

        group.MapGet("/", async (IArticleReader articles, CancellationToken ct) =>
                Results.Ok(await articles.GetAllAsync(ct)))
            .WithName("ListArticles")
            .WithSummary("Article list (metadata only), sorted by date descending");

        // CQRS/MediatR demo #1 — see README §1.2: the pattern on purpose, not everywhere.
        group.MapGet("/{slug}", async (string slug, IMediator mediator, CancellationToken ct) =>
            {
                var article = await mediator.Send(new GetArticleBySlugQuery(slug), ct);
                return article is null
                    ? Results.Problem(
                        title: "Article not found",
                        detail: $"No article with slug '{slug}'.",
                        statusCode: StatusCodes.Status404NotFound)
                    : Results.Ok(article);
            })
            .WithName("GetArticleBySlug")
            .WithSummary("Full article: rendered HTML + raw markdown")
            .WithDescription("CQRS/MediatR demo endpoint #1.");

        return app;
    }
}
