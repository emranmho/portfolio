using System.Reflection;

namespace Portfolio.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly DateTime StartedAtUtc = DateTime.UtcNow;

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", (IConfiguration config) =>
            {
                var version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion ?? "unknown";

                return Results.Ok(new
                {
                    status = "ok",
                    version,
                    gitSha = config["GIT_SHA"] ?? "dev",
                    startedAtUtc = StartedAtUtc,
                    uptimeSeconds = Math.Round((DateTime.UtcNow - StartedAtUtc).TotalSeconds, 1),
                });
            })
            .WithName("GetHealth")
            .WithTags("Ops")
            .WithSummary("Liveness + version + git SHA")
            .WithDescription("Used by the Docker healthcheck and the deploy gate. Excluded from metrics and rate limiting.");

        return app;
    }
}
