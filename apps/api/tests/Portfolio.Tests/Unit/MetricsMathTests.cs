using FluentAssertions;
using Portfolio.Infrastructure.Metrics;

namespace Portfolio.Tests.Unit;

public class MetricsMathTests
{
    [Fact]
    public void Empty_sample_returns_zero() =>
        MetricsMath.Percentile([], 95).Should().Be(0);

    [Fact]
    public void Single_sample_is_every_percentile()
    {
        MetricsMath.Percentile([42.0], 50).Should().Be(42.0);
        MetricsMath.Percentile([42.0], 99).Should().Be(42.0);
    }

    [Fact]
    public void Nearest_rank_over_1_to_100()
    {
        var samples = Enumerable.Range(1, 100).Select(i => (double)i).ToList();

        MetricsMath.Percentile(samples, 50).Should().Be(50);
        MetricsMath.Percentile(samples, 95).Should().Be(95);
        MetricsMath.Percentile(samples, 99).Should().Be(99);
        MetricsMath.Percentile(samples, 100).Should().Be(100);
    }
}
