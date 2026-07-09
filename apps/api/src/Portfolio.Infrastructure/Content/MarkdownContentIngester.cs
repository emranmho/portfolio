using System.Text.Json;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Domain;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Content;

/// <summary>
/// Reads content/ (markdown articles + JSON project definitions) at startup and
/// upserts everything into SQLite. Content in git is the source of truth:
/// rows whose files disappeared are deleted.
/// </summary>
public sealed class MarkdownContentIngester(
    PortfolioDbContext db,
    ILogger<MarkdownContentIngester> logger)
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task IngestAsync(string contentRoot, CancellationToken ct = default)
    {
        var articles = await IngestArticlesAsync(Path.Combine(contentRoot, "articles"), ct);
        var projects = await IngestProjectsAsync(Path.Combine(contentRoot, "projects"), ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Content ingested: {Articles} articles, {Projects} projects from {Root}",
            articles, projects, contentRoot);
    }

    public async Task RecordDeployAsync(string gitSha, CancellationToken ct = default)
    {
        var latest = await db.Deploys.OrderByDescending(d => d.DeployedAtUtc).FirstOrDefaultAsync(ct);
        if (latest?.GitSha == gitSha)
            return;

        db.Deploys.Add(new Deploy { GitSha = gitSha, DeployedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("New deploy recorded: {GitSha}", gitSha);
    }

    private async Task<int> IngestArticlesAsync(string dir, CancellationToken ct)
    {
        var seen = new List<string>();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(f => f))
            {
                var slug = Slug.Create(Path.GetFileNameWithoutExtension(file)).Value;
                var (fm, body) = FrontmatterParser.Parse(await File.ReadAllTextAsync(file, ct));

                var article = await db.Articles.FirstOrDefaultAsync(a => a.Slug == slug, ct)
                    ?? db.Articles.Add(new Article { Slug = slug, Title = slug }).Entity;

                article.Title = fm.Title ?? slug;
                article.Summary = fm.Summary ?? "";
                article.Tags = fm.Tags ?? [];
                article.PublishedAtUtc = DateTime.SpecifyKind(fm.Date ?? File.GetLastWriteTimeUtc(file), DateTimeKind.Utc);
                article.RawMarkdown = body;
                article.Html = Markdown.ToHtml(body, Pipeline);
                article.ReadingTimeMinutes = EstimateReadingMinutes(body);

                seen.Add(slug);
            }
        }

        db.Articles.RemoveRange(db.Articles.Where(a => !seen.Contains(a.Slug)));
        return seen.Count;
    }

    private async Task<int> IngestProjectsAsync(string dir, CancellationToken ct)
    {
        var seen = new List<string>();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var model = JsonSerializer.Deserialize<ProjectFile>(json, JsonOptions)
                    ?? throw new InvalidDataException($"Empty project definition: {file}");

                var slug = Slug.Create(model.Slug ?? Path.GetFileNameWithoutExtension(file)).Value;

                var project = await db.Projects.FirstOrDefaultAsync(p => p.Slug == slug, ct)
                    ?? db.Projects.Add(new Project { Slug = slug, Name = slug, Summary = "" }).Entity;

                project.Name = model.Name ?? slug;
                project.Summary = model.Summary ?? "";
                project.Description = model.Description ?? "";
                project.Stack = model.Stack ?? [];
                project.RepoUrl = model.RepoUrl;
                project.LiveUrl = model.LiveUrl;
                project.Featured = model.Featured;
                project.SortOrder = model.Order;

                seen.Add(slug);
            }
        }

        db.Projects.RemoveRange(db.Projects.Where(p => !seen.Contains(p.Slug)));
        return seen.Count;
    }

    internal static int EstimateReadingMinutes(string markdown)
    {
        var words = markdown.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(words / 200.0));
    }

    private sealed class ProjectFile
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public List<string>? Stack { get; set; }
        public string? RepoUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
    }
}
