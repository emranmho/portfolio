using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", async (string? q, ISearchService search, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Results.Problem(
                        title: "Missing query",
                        detail: "Provide a search term: /api/search?q=sqlite",
                        statusCode: StatusCodes.Status400BadRequest);

                var results = await search.SearchAsync(q, ct);
                return Results.Ok(new { query = q, count = results.Count, results });
            })
            .WithName("Search")
            .WithTags("Search")
            .WithSummary("Full-text search over articles and projects (SQLite FTS5)")
            .WithDescription("bm25-ranked, prefix-matching, porter-stemmed. Matches are wrapped in <mark> tags. Deliberately not Elasticsearch — see the trade-offs philosophy in the README.");

        return app;
    }
}
