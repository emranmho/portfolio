namespace Portfolio.Domain;

/// <summary>Per-minute, per-route aggregate persisted to SQLite by the flush service.</summary>
public sealed class MetricBucket
{
    public int Id { get; set; }
    public DateTime BucketStartUtc { get; set; }
    public required string Route { get; set; }
    public long Count { get; set; }
    public long ErrorCount { get; set; }
    public double AvgMs { get; set; }
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
}
