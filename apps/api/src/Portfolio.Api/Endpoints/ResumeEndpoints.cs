using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Endpoints;

public static class ResumeEndpoints
{
    public static IEndpointRouteBuilder MapResumeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/resume", async (IResumeReader resume, CancellationToken ct) =>
                Results.Content(await resume.GetJsonAsync(ct), "application/json"))
            .WithName("GetResume")
            .WithTags("Content")
            .WithSummary("Structured resume JSON + PDF link")
            .WithDescription("Served verbatim from content/resume.json in the repo. My resume is an API endpoint — the PDF at `pdf` is the human-readable fallback.");

        return app;
    }
}
