using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Portfolio.Tests.Integration;

/// <summary>
/// Boots the real HTTP pipeline against a shared-cache in-memory SQLite database
/// and a temp content directory with known fixture files.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _contentDir;
    private readonly int _rateLimitPermit;

    public ApiFactory() : this(rateLimitPermit: 100_000) { }

    protected ApiFactory(int rateLimitPermit)
    {
        _rateLimitPermit = rateLimitPermit;

        var connectionString = $"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();
        ConnectionString = connectionString;

        _contentDir = Path.Combine(Path.GetTempPath(), $"portfolio-tests-{Guid.NewGuid():N}");
        WriteFixtureContent(_contentDir);
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Db", ConnectionString);
        builder.UseSetting("Content:Root", _contentDir);
        builder.UseSetting("RateLimiting:PermitLimit", _rateLimitPermit.ToString());
        builder.UseSetting("RateLimiting:WindowSeconds", "60");
        builder.UseSetting("GIT_SHA", "test-sha");
    }

    private static void WriteFixtureContent(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "articles"));
        Directory.CreateDirectory(Path.Combine(root, "projects"));

        File.WriteAllText(Path.Combine(root, "whoami.json"),
            """{ "name": "Test Emran", "role": "Backend Engineer" }""");

        File.WriteAllText(Path.Combine(root, "resume.json"),
            """{ "name": "Test Emran", "role": "Backend Engineer", "pdf": "https://example.com/resume.pdf" }""");

        File.WriteAllText(Path.Combine(root, "articles", "test-article.md"),
            """
            ---
            title: "Test Article"
            summary: "Integration fixture."
            date: 2026-01-15
            tags: [test]
            ---

            # Test Article

            Some **markdown** body.
            """);

        File.WriteAllText(Path.Combine(root, "projects", "test-project.json"),
            """
            {
              "slug": "test-project",
              "name": "Test Project",
              "summary": "Fixture project",
              "description": "Longer description.",
              "stack": ["dotnet", "sqlite"],
              "repoUrl": "https://example.com/repo",
              "featured": true,
              "order": 1
            }
            """);

        File.WriteAllText(Path.Combine(root, "projects", "go-project.json"),
            """
            {
              "slug": "go-project",
              "name": "Go Project",
              "summary": "Fixture Go project",
              "stack": ["go"],
              "order": 2
            }
            """);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
            try { Directory.Delete(_contentDir, recursive: true); } catch { /* temp dir, best effort */ }
        }
    }
}

/// <summary>Factory with a tiny rate limit so the 429 path is testable in isolation.</summary>
public sealed class LowRateLimitApiFactory() : ApiFactory(rateLimitPermit: 3);
