using System.Diagnostics;
using Portfolio.Api.Live;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;

namespace Portfolio.Api.Middleware;

/// <summary>
/// Records route template, status code and elapsed ms for every /api request.
/// /api/health is excluded so the Docker healthcheck doesn't inflate traffic;
/// /api/live is excluded because an open SSE stream would register as one
/// request lasting hours. Samples also fan out to the live feed.
/// </summary>
public sealed class MetricsMiddleware(RequestDelegate next, IMetricsStore store, LiveRequestFeed feed)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/api/health")
            || context.Request.Path.StartsWithSegments("/api/live"))
        {
            await next(context);
            return;
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            // Route template keeps cardinality bounded: /api/articles/{slug}, not every slug.
            var route = NormalizeRoute((context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
                ?? "unmatched");
            store.Record(new MetricSample(
                $"{context.Request.Method} {route}",
                context.Response.StatusCode,
                elapsed,
                DateTime.UtcNow));
            feed.Publish(new LiveRequestEvent(
                context.Request.Method,
                route,
                context.Response.StatusCode,
                Math.Round(elapsed, 1),
                DateTime.UtcNow));
        }
    }

    private static string NormalizeRoute(string route)
    {
        if (!route.StartsWith('/'))
            route = "/" + route;
        // Group-relative "/" templates render as "/api/projects/" — drop the trailing slash.
        return route.Length > 1 ? route.TrimEnd('/') : route;
    }
}
