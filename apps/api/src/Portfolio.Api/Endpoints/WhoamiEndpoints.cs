using Portfolio.Application.Abstractions;

namespace Portfolio.Api.Endpoints;

public static class WhoamiEndpoints
{
    public static IEndpointRouteBuilder MapWhoamiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/whoami", async (IWhoamiReader whoami, CancellationToken ct) =>
                Results.Content(await whoami.GetJsonAsync(ct), "application/json"))
            .WithName("GetWhoami")
            .WithTags("Content")
            .WithSummary("Identity JSON — the terminal-hero payload")
            .WithDescription("Served verbatim from content/whoami.json in the repo.");

        return app;
    }
}
