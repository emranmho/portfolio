namespace Portfolio.Application.Features.Metrics.GetMetricsSummary;

public sealed record GetMetricsSummaryResponse(
    DateTime ProcessStartedUtc,
    double UptimeSeconds,
    long RequestsToday,
    double AvgLatencyMs,
    double P95LatencyMs,
    double ErrorRatePercent,
    IReadOnlyList<EndpointMetricsDto> Endpoints,
    IReadOnlyList<DeployDto> Deploys);
