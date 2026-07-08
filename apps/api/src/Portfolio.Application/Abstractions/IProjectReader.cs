namespace Portfolio.Application.Abstractions;

public interface IProjectReader
{
    /// <param name="stack">Optional case-insensitive stack filter, e.g. "go" or "dotnet".</param>
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(string? stack, CancellationToken ct);

    Task<ProjectDto?> GetBySlugAsync(string slug, CancellationToken ct);
}
