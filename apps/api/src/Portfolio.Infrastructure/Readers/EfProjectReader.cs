using Microsoft.EntityFrameworkCore;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Domain;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Readers;

public sealed class EfProjectReader(PortfolioDbContext db) : IProjectReader
{
    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(string? stack, CancellationToken ct)
    {
        // Stack is a JSON-serialized column; the dataset is tiny, filter in memory.
        var projects = await db.Projects.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(stack))
            projects = projects
                .Where(p => p.Stack.Any(s => s.Equals(stack, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), ct);
        return project is null ? null : ToDto(project);
    }

    private static ProjectDto ToDto(Project p) =>
        new(p.Slug, p.Name, p.Summary, p.Description, p.DescriptionHtml, p.Stack, p.RepoUrl, p.LiveUrl, p.Featured);
}
