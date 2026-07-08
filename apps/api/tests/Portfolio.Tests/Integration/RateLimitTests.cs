using System.Net;
using FluentAssertions;

namespace Portfolio.Tests.Integration;

public class RateLimitTests(LowRateLimitApiFactory factory) : IClassFixture<LowRateLimitApiFactory>
{
    [Fact]
    public async Task Exceeding_the_window_returns_429_with_retry_after()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/api/projects")).StatusCode.Should().Be(HttpStatusCode.OK,
                $"request {i + 1} is within the permit limit");

        var rejected = await client.GetAsync("/api/projects");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().ContainKey("Retry-After");
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Health_is_never_rate_limited()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
            (await client.GetAsync("/api/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
