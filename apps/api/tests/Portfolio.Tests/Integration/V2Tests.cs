using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Portfolio.Tests.Integration;

public class V2Tests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task V2_projects_returns_paginated_envelope_with_meta_and_links()
    {
        var response = await _client.GetAsync("/api/v2/projects?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Api-Version").Single().Should().Be("2");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("meta").GetProperty("totalItems").GetInt32().Should().Be(2);
        body.GetProperty("meta").GetProperty("totalPages").GetInt32().Should().Be(2);
        body.GetProperty("links").GetProperty("next").GetString().Should().Be("/api/v2/projects?page=2&pageSize=1");
        body.GetProperty("links").GetProperty("prev").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task V1_projects_still_returns_bare_array()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/projects");

        body.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
