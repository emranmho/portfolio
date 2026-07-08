using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Portfolio.Tests.Integration;

public class EndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok_with_git_sha()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("ok");
        body.GetProperty("gitSha").GetString().Should().Be("test-sha");
        body.GetProperty("uptimeSeconds").GetDouble().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Whoami_serves_content_file_verbatim()
    {
        var response = await _client.GetAsync("/api/whoami");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Test Emran");
    }

    [Fact]
    public async Task Resume_serves_content_file_verbatim()
    {
        var response = await _client.GetAsync("/api/resume");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Test Emran");
        body.GetProperty("pdf").GetString().Should().Be("https://example.com/resume.pdf");
    }

    [Fact]
    public async Task Projects_list_returns_ingested_projects()
    {
        var projects = await _client.GetFromJsonAsync<JsonElement>("/api/projects");

        projects.GetArrayLength().Should().Be(2);
        projects[0].GetProperty("slug").GetString().Should().Be("test-project");
    }

    [Fact]
    public async Task Projects_stack_filter_is_case_insensitive()
    {
        var projects = await _client.GetFromJsonAsync<JsonElement>("/api/projects?stack=GO");

        projects.GetArrayLength().Should().Be(1);
        projects[0].GetProperty("slug").GetString().Should().Be("go-project");
    }

    [Fact]
    public async Task Project_detail_returns_full_payload()
    {
        var project = await _client.GetFromJsonAsync<JsonElement>("/api/projects/test-project");

        project.GetProperty("name").GetString().Should().Be("Test Project");
        project.GetProperty("stack").EnumerateArray().Select(s => s.GetString())
            .Should().Contain("dotnet");
    }

    [Fact]
    public async Task Unknown_project_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/projects/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Project not found");
    }

    [Fact]
    public async Task Articles_list_returns_metadata_only()
    {
        var articles = await _client.GetFromJsonAsync<JsonElement>("/api/articles");

        articles.GetArrayLength().Should().Be(1);
        var article = articles[0];
        article.GetProperty("title").GetString().Should().Be("Test Article");
        article.GetProperty("readingTimeMinutes").GetInt32().Should().BeGreaterThan(0);
        article.TryGetProperty("html", out _).Should().BeFalse("list endpoint is metadata only");
    }

    [Fact]
    public async Task Article_detail_returns_rendered_html_and_raw_markdown()
    {
        var article = await _client.GetFromJsonAsync<JsonElement>("/api/articles/test-article");

        article.GetProperty("html").GetString().Should().Contain("<strong>markdown</strong>");
        article.GetProperty("rawMarkdown").GetString().Should().Contain("**markdown**");
    }

    [Fact]
    public async Task Unknown_article_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/articles/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Metrics_reflect_recorded_traffic_and_deploy_history()
    {
        await _client.GetAsync("/api/projects");

        var metrics = await _client.GetFromJsonAsync<JsonElement>("/api/metrics");

        metrics.GetProperty("requestsToday").GetInt64().Should().BeGreaterThan(0);
        metrics.GetProperty("uptimeSeconds").GetDouble().Should().BeGreaterThan(0);
        metrics.GetProperty("endpoints").EnumerateArray()
            .Select(e => e.GetProperty("route").GetString())
            .Should().Contain("GET /api/projects");
        metrics.GetProperty("deploys").EnumerateArray()
            .Select(d => d.GetProperty("gitSha").GetString())
            .Should().Contain("test-sha");
    }

    [Fact]
    public async Task Api_responses_expose_rate_limit_headers()
    {
        var response = await _client.GetAsync("/api/projects");

        response.Headers.Should().ContainKey("X-RateLimit-Limit");
        response.Headers.Should().ContainKey("X-RateLimit-Window");
    }

    [Fact]
    public async Task Docs_and_openapi_render()
    {
        (await _client.GetAsync("/docs")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Openapi_document_lists_the_live_feed_route()
    {
        // Full-duplex SSE isn't exercisable through WebApplicationFactory's in-memory
        // TestServer (verified manually against a running instance instead — see
        // LiveRequestFeedTests for the publish/subscribe behavior itself); this at
        // least proves the endpoint is wired into routing.
        var openapi = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        openapi.GetProperty("paths").TryGetProperty("/api/live/requests", out _).Should().BeTrue();
    }
}
