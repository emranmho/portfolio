using Portfolio.Domain;

namespace Portfolio.Application.Abstractions;

public interface IMetricsStore
{
    /// <summary>Buffers one request sample in memory. Must be cheap and non-blocking.</summary>
    void Record(MetricSample sample);

    /// <summary>Drains the buffer and persists per-minute aggregates.</summary>
    Task FlushAsync(CancellationToken ct);

    /// <summary>Flushes pending samples, then aggregates persisted buckets into a summary.</summary>
    Task<MetricsSummaryDto> GetSummaryAsync(CancellationToken ct);
}
