using MediatR;
using Portfolio.Application.Abstractions;

namespace Portfolio.Application.Features.Metrics.GetMetricsSummary;

public sealed class GetMetricsSummaryHandler(IMetricsStore metrics)
    : IRequestHandler<GetMetricsSummaryQuery, GetMetricsSummaryResponse>
{
    public async Task<GetMetricsSummaryResponse> Handle(GetMetricsSummaryQuery request, CancellationToken ct)
    {
        var summary = await metrics.GetSummaryAsync(ct);

        return new GetMetricsSummaryResponse(
            summary.ProcessStartedUtc,
            summary.UptimeSeconds,
            summary.RequestsToday,
            summary.AvgLatencyMs,
            summary.P95LatencyMs,
            summary.ErrorRatePercent,
            summary.Endpoints,
            summary.Deploys);
    }
}
