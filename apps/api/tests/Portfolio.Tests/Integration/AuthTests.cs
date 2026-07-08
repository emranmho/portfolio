using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Portfolio.Tests.Integration;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Secret_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/secret");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_endpoint_issues_15_minute_bearer_token()
    {
        var response = await _client.PostAsync("/api/auth/token", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tokenType").GetString().Should().Be("Bearer");
        body.GetProperty("expiresInSeconds").GetInt32().Should().Be(900);
        body.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Secret_with_token_returns_200_and_echoes_claims()
    {
        var tokenResponse = await _client.PostAsync("/api/auth/token", content: null);
        var token = (await tokenResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/secret");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("subject").GetString().Should().StartWith("guest-");
        body.GetProperty("claims").EnumerateArray()
            .Select(c => c.GetProperty("value").GetString())
            .Should().Contain("explorer");
    }
}
