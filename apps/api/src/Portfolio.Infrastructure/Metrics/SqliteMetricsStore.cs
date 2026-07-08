using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Metrics;

/// <summary>
/// Singleton. Requests are buffered in memory by the middleware (Record) and flushed
/// as per-minute, per-route aggregates into SQLite (FlushAsync) every 30s by
/// <see cref="MetricsFlushService"/>. Summaries flush first so numbers are current.
/// </summary>
public sealed class SqliteMetricsStore(IServiceScopeFactory scopeFactory) : IMetricsStore
{
    private static readonly DateTime ProcessStartedUtc =
        DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);

    private readonly ConcurrentQueue<MetricSample> _buffer = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    public void Record(MetricSample sample) => _buffer.Enqueue(sample);

    public async Task FlushAsync(CancellationToken ct)
    {
        await _flushLock.WaitAsync(ct);
        try
        {
            var samples = new List<MetricSample>();
            while (_buffer.TryDequeue(out var s))
                samples.Add(s);
            if (samples.Count == 0)
                return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

            foreach (var group in samples.GroupBy(s => (
                Bucket: new DateTime(s.TimestampUtc.Year, s.TimestampUtc.Month, s.TimestampUtc.Day,
                    s.TimestampUtc.Hour, s.TimestampUtc.Minute, 0, DateTimeKind.Utc),
                s.Route)))
            {
                var elapsed = group.Select(s => s.ElapsedMs).OrderBy(v => v).ToList();
                var count = elapsed.Count;
                var errors = group.LongCount(s => s.IsError);
                var avg = elapsed.Average();
                var p50 = MetricsMath.Percentile(elapsed, 50);
                var p95 = MetricsMath.Percentile(elapsed, 95);
                var p99 = MetricsMath.Percentile(elapsed, 99);

                var bucket = await db.MetricBuckets.FirstOrDefaultAsync(
                    b => b.BucketStartUtc == group.Key.Bucket && b.Route == group.Key.Route, ct);

                if (bucket is null)
                {
                    db.MetricBuckets.Add(new MetricBucket
                    {
                        BucketStartUtc = group.Key.Bucket,
                        Route = group.Key.Route,
                        Count = count,
                        ErrorCount = errors,
                        AvgMs = avg,
                        P50Ms = p50,
                        P95Ms = p95,
                        P99Ms = p99,
                    });
                }
                else
                {
                    // Same minute flushed twice: merge counts exactly, percentiles as
                    // count-weighted averages (estimation, consistent with the README).
                    var total = bucket.Count + count;
                    bucket.AvgMs = Weighted(bucket.AvgMs, bucket.Count, avg, count, total);
                    bucket.P50Ms = Weighted(bucket.P50Ms, bucket.Count, p50, count, total);
                    bucket.P95Ms = Weighted(bucket.P95Ms, bucket.Count, p95, count, total);
                    bucket.P99Ms = Weighted(bucket.P99Ms, bucket.Count, p99, count, total);
                    bucket.Count = total;
                    bucket.ErrorCount += errors;
                }
            }

            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public async Task<MetricsSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        await FlushAsync(ct);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        var buckets = await db.MetricBuckets.AsNoTracking().ToListAsync(ct);
        var todayStart = DateTime.UtcNow.Date;

        var totalCount = buckets.Sum(b => b.Count);
        var totalErrors = buckets.Sum(b => b.ErrorCount);

        var endpoints = buckets
            .GroupBy(b => b.Route)
            .Select(g =>
            {
                var count = g.Sum(b => b.Count);
                return new EndpointMetricsDto(
                    g.Key,
                    count,
                    g.Sum(b => b.ErrorCount),
                    WeightedAvg(g, b => b.AvgMs, count),
                    WeightedAvg(g, b => b.P50Ms, count),
                    WeightedAvg(g, b => b.P95Ms, count),
                    WeightedAvg(g, b => b.P99Ms, count));
            })
            .OrderByDescending(e => e.Count)
            .ToList();

        var deploys = await db.Deploys.AsNoTracking()
            .OrderByDescending(d => d.DeployedAtUtc)
            .Take(10)
            .Select(d => new DeployDto(d.GitSha, d.DeployedAtUtc))
            .ToListAsync(ct);

        return new MetricsSummaryDto(
            ProcessStartedUtc,
            Math.Round((DateTime.UtcNow - ProcessStartedUtc).TotalSeconds, 1),
            buckets.Where(b => b.BucketStartUtc >= todayStart).Sum(b => b.Count),
            Round(totalCount == 0 ? 0 : buckets.Sum(b => b.AvgMs * b.Count) / totalCount),
            Round(totalCount == 0 ? 0 : buckets.Sum(b => b.P95Ms * b.Count) / totalCount),
            Round(totalCount == 0 ? 0 : 100.0 * totalErrors / totalCount),
            endpoints,
            deploys);
    }

    private static double Weighted(double a, long aCount, double b, long bCount, long total) =>
        total == 0 ? 0 : (a * aCount + b * bCount) / total;

    private static double WeightedAvg(IEnumerable<MetricBucket> buckets, Func<MetricBucket, double> value, long total) =>
        total == 0 ? 0 : Round(buckets.Sum(b => value(b) * b.Count) / total);

    private static double Round(double v) => Math.Round(v, 2);
}
