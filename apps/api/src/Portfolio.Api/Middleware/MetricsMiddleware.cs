using System.Diagnostics;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;

namespace Portfolio.Api.Middleware;

/// <summary>
/// Records route template, status code and elapsed ms for every /api request.
/// /api/health is excluded so the Docker healthcheck doesn't inflate traffic.
/// </summary>
public sealed class MetricsMiddleware(RequestDelegate next, IMetricsStore store)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/api/health"))
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
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
                ?? "unmatched";
            store.Record(new MetricSample(
                $"{context.Request.Method} {NormalizeRoute(route)}",
                context.Response.StatusCode,
                elapsed,
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
