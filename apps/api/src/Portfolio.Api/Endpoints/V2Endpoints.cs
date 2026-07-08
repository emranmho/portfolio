using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Endpoints;

/// <summary>
/// API versioning demo — see README. v1 returns a bare array and stays as-is
/// (breaking existing consumers to add pagination would be the real failure).
/// v2 shows the evolution: an envelope with meta + links, versioned in the URL
/// segment because it's visible, cacheable, and curl-able — the right trade-off
/// for a public API this size versus header or media-type versioning.
/// </summary>
public static class V2Endpoints
{
    public static IEndpointRouteBuilder MapV2Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2").WithTags("v2 (versioning demo)");

        group.MapGet("/projects", async (
                int? page, int? pageSize, IProjectReader projects, HttpContext http, CancellationToken ct) =>
            {
                var all = await projects.GetAllAsync(stack: null, ct);
                var p = Math.Max(1, page ?? 1);
                var size = Math.Clamp(pageSize ?? 10, 1, 50);
                var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)size));

                http.Response.Headers["X-Api-Version"] = "2";
                return Results.Ok(new
                {
                    data = all.Skip((p - 1) * size).Take(size),
                    meta = new { page = p, pageSize = size, totalItems = all.Count, totalPages },
                    links = new
                    { 
                        self = Link(p, size),
                        next = p < totalPages ? Link(p + 1, size) : null,
                        prev = p > 1 ? Link(p - 1, size) : null,
                    },
                });
            })
            .WithName("ListProjectsV2")
            .WithSummary("v2 project list: paginated envelope instead of v1's bare array")
            .WithDescription("The versioning demo. v1 (/api/projects) returns a bare array and must never break; v2 wraps data in an envelope with meta and links so pagination could grow without another breaking change. URL-segment versioning on purpose: visible, cacheable, curl-able.");

        return app;

        static string Link(int page, int size) => $"/api/v2/projects?page={page}&pageSize={size}";
    }
}
