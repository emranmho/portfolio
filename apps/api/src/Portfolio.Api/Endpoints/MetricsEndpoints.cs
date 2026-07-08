using MediatR;
using Portfolio.Application.Features.Metrics.GetMetricsSummary;

namespace Portfolio.Api.Endpoints;

public static class MetricsEndpoints
{
    public static IEndpointRouteBuilder MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        // CQRS/MediatR demo #2 — see README §1.2.
        app.MapGet("/api/metrics", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetMetricsSummaryQuery(), ct)))
            .WithName("GetMetrics")
            .WithTags("Ops")
            .WithSummary("Real request counts, latency percentiles, uptime, deploy history")
            .WithDescription("Collected by custom middleware into SQLite — the honest status page. CQRS/MediatR demo endpoint #2.");

        return app;
    }
}
