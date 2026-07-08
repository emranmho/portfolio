namespace Portfolio.Application.Abstractions;

public interface ISearchService
{
    /// <summary>Drops and repopulates the full-text index from ingested content.</summary>
    Task RebuildIndexAsync(CancellationToken ct);

    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, CancellationToken ct);
}
