namespace Portfolio.Infrastructure.Metrics;

public static class MetricsMath
{
    /// <summary>
    /// Nearest-rank percentile over a sorted sample. Simple estimation on purpose —
    /// per-minute buckets never hold enough samples to justify anything fancier.
    /// </summary>
    public static double Percentile(IReadOnlyList<double> sortedAscending, double percentile)
    {
        if (sortedAscending.Count == 0)
            return 0;

        var rank = (int)Math.Ceiling(percentile / 100.0 * sortedAscending.Count) - 1;
        return sortedAscending[Math.Clamp(rank, 0, sortedAscending.Count - 1)];
    }
}
