namespace Portfolio.Domain;

/// <summary>One raw request measurement, buffered in memory before aggregation.</summary>
public readonly record struct MetricSample(
    string Route,
    int StatusCode,
    double ElapsedMs,
    DateTime TimestampUtc)
{
    public bool IsError => StatusCode >= 400;
}
