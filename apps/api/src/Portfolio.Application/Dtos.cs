namespace Portfolio.Application;

public sealed record ProjectDto(
    string Slug,
    string Name,
    string Summary,
    string Description,
    IReadOnlyList<string> Stack,
    string? RepoUrl,
    string? LiveUrl,
    bool Featured);

public sealed record ArticleSummaryDto(
    string Slug,
    string Title,
    string Summary,
    DateTime PublishedAtUtc,
    IReadOnlyList<string> Tags,
    int ReadingTimeMinutes);

public sealed record ArticleDetailDto(
    string Slug,
    string Title,
    string Summary,
    DateTime PublishedAtUtc,
    IReadOnlyList<string> Tags,
    int ReadingTimeMinutes,
    string Html,
    string RawMarkdown);

public sealed record EndpointMetricsDto(
    string Route,
    long Count,
    long ErrorCount,
    double AvgMs,
    double P50Ms,
    double P95Ms,
    double P99Ms);

public sealed record DeployDto(string GitSha, DateTime DeployedAtUtc);

public sealed record MetricsSummaryDto(
    DateTime ProcessStartedUtc,
    double UptimeSeconds,
    long RequestsToday,
    double AvgLatencyMs,
    double P95LatencyMs,
    double ErrorRatePercent,
    IReadOnlyList<EndpointMetricsDto> Endpoints,
    IReadOnlyList<DeployDto> Deploys);

/// <summary>Snippet wraps matches in &lt;mark&gt; tags; Type is "article" or "project".</summary>
public sealed record SearchResultDto(
    string Type,
    string Slug,
    string Title,
    string Snippet);
