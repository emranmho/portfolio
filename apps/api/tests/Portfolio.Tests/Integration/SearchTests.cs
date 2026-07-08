using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Portfolio.Tests.Integration;

public class SearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Search_finds_article_by_body_text_with_highlighted_snippet()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/search?q=markdown");

        body.GetProperty("query").GetString().Should().Be("markdown");
        body.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
        var first = body.GetProperty("results")[0];
        first.GetProperty("type").GetString().Should().Be("article");
        first.GetProperty("slug").GetString().Should().Be("test-article");
        first.GetProperty("snippet").GetString().Should().Contain("<mark>markdown</mark>");
    }

    [Fact]
    public async Task Search_finds_project_by_name_prefix()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/search?q=fixtur");

        body.GetProperty("results").EnumerateArray()
            .Select(r => r.GetProperty("slug").GetString())
            .Should().Contain("test-project");
    }

    [Fact]
    public async Task Search_without_query_returns_400_problem_details()
    {
        var response = await _client.GetAsync("/api/search");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Search_neutralizes_fts5_operator_syntax_instead_of_500ing()
    {
        var response = await _client.GetAsync("/api/search?q=" + Uri.EscapeDataString("AND OR (\" NEAR"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_with_no_matches_returns_empty_results()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/search?q=zzzznothing");

        body.GetProperty("count").GetInt32().Should().Be(0);
        body.GetProperty("results").GetArrayLength().Should().Be(0);
    }
}
