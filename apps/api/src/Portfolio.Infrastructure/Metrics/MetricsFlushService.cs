using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Abstractions;

namespace Portfolio.Infrastructure.Metrics;

/// <summary>Flushes the in-memory metrics buffer to SQLite every 30 seconds.</summary>
public sealed class MetricsFlushService(IMetricsStore store, ILogger<MetricsFlushService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await WaitAsync(timer, stoppingToken))
        {
            try
            {
                await store.FlushAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Metrics flush failed; samples stay buffered for the next cycle");
            }
        }

        // Best effort on shutdown so short-lived runs still persist their traffic.
        try
        {
            await store.FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Final metrics flush on shutdown failed");
        }
    }

    private static async ValueTask<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
