using MediatR;

namespace Portfolio.Application.Features.Metrics.GetMetricsSummary;

public sealed record GetMetricsSummaryQuery : IRequest<GetMetricsSummaryResponse>;
