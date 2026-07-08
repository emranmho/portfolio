using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Content");

        group.MapGet("/", async (string? stack, IProjectReader projects, CancellationToken ct) =>
                Results.Ok(await projects.GetAllAsync(stack, ct)))
            .WithName("ListProjects")
            .WithSummary("List projects")
            .WithDescription("Optional ?stack= filter (case-insensitive), e.g. ?stack=go or ?stack=dotnet.");

        group.MapGet("/{slug}", async (string slug, IProjectReader projects, CancellationToken ct) =>
            {
                var project = await projects.GetBySlugAsync(slug, ct);
                return project is null
                    ? Results.Problem(
                        title: "Project not found",
                        detail: $"No project with slug '{slug}'.",
                        statusCode: StatusCodes.Status404NotFound)
                    : Results.Ok(project);
            })
            .WithName("GetProjectBySlug")
            .WithSummary("Project detail")
            .WithDescription("404 with ProblemDetails when the slug is unknown.");

        return app;
    }
}
