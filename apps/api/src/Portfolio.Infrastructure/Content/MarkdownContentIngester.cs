using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Domain;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Content;

/// <summary>
/// Reads content/ (markdown articles + markdown project definitions) at startup and
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
                var (fm, body) = FrontmatterParser.Parse<ArticleFrontmatter>(await File.ReadAllTextAsync(file, ct));

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
            foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(f => f))
            {
                var slug = Slug.Create(Path.GetFileNameWithoutExtension(file)).Value;
                var (fm, body) = FrontmatterParser.Parse<ProjectFrontmatter>(await File.ReadAllTextAsync(file, ct));

                var project = await db.Projects.FirstOrDefaultAsync(p => p.Slug == slug, ct)
                    ?? db.Projects.Add(new Project { Slug = slug, Name = slug, Summary = "" }).Entity;

                project.Name = fm.Name ?? slug;
                project.Summary = fm.Summary ?? "";
                project.Description = body;
                project.DescriptionHtml = Markdown.ToHtml(body, Pipeline);
                project.Stack = fm.Stack ?? [];
                project.RepoUrl = fm.RepoUrl;
                project.LiveUrl = fm.LiveUrl;
                project.Featured = fm.Featured;
                project.SortOrder = fm.Order;

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
}
