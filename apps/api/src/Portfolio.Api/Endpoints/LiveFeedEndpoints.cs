using System.Net.ServerSentEvents;
using Portfolio.Api.Live;

namespace Portfolio.Api.Endpoints;

public static class LiveFeedEndpoints
{
    public static IEndpointRouteBuilder MapLiveFeedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/live/requests", (LiveRequestFeed feed, CancellationToken ct) =>
            {
                var events = feed.Subscribe(ct)
                    .Select(e => new SseItem<LiveRequestEvent>(e, "request"));
                return TypedResults.ServerSentEvents(events);
            })
            .WithName("LiveRequestFeed")
            .WithTags("Live")
            .WithSummary("Server-Sent Events stream of live API traffic")
            .WithDescription("Anonymized: route template, status, and latency only — no IPs, no payloads. Open it with curl -N or an EventSource; every request anyone makes to the API shows up in real time.");

        return app;
    }
}
