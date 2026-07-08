using FluentAssertions;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Features.Articles.GetArticleBySlug;

namespace Portfolio.Tests.Unit;

public class GetArticleBySlugHandlerTests
{
    [Fact]
    public async Task Returns_mapped_response_when_article_exists()
    {
        var detail = new ArticleDetailDto(
            "hello", "Hello", "Summary", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            ["meta"], 3, "<p>hi</p>", "hi");
        var handler = new GetArticleBySlugHandler(new FakeArticleReader(detail));

        var response = await handler.Handle(new GetArticleBySlugQuery("hello"), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Slug.Should().Be("hello");
        response.Html.Should().Be("<p>hi</p>");
        response.RawMarkdown.Should().Be("hi");
    }

    [Fact]
    public async Task Returns_null_when_article_missing()
    {
        var handler = new GetArticleBySlugHandler(new FakeArticleReader(null));

        var response = await handler.Handle(new GetArticleBySlugQuery("nope"), CancellationToken.None);

        response.Should().BeNull();
    }

    private sealed class FakeArticleReader(ArticleDetailDto? detail) : IArticleReader
    {
        public Task<IReadOnlyList<ArticleSummaryDto>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ArticleSummaryDto>>([]);

        public Task<ArticleDetailDto?> GetBySlugAsync(string slug, CancellationToken ct) =>
            Task.FromResult(detail);
    }
}
