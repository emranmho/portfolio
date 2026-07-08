using FluentAssertions;
using Portfolio.Infrastructure.Content;

namespace Portfolio.Tests.Unit;

public class FrontmatterParserTests
{
    [Fact]
    public void Parses_frontmatter_and_body()
    {
        const string markdown = """
            ---
            title: "My Post"
            summary: "A summary."
            date: 2026-07-01
            tags: [dotnet, meta]
            ---

            # Heading

            Body text.
            """;

        var (fm, body) = FrontmatterParser.Parse(markdown);

        fm.Title.Should().Be("My Post");
        fm.Summary.Should().Be("A summary.");
        fm.Date.Should().Be(new DateTime(2026, 7, 1));
        fm.Tags.Should().Equal("dotnet", "meta");
        body.Should().Contain("# Heading").And.Contain("Body text.");
        body.Should().NotContain("title:");
    }

    [Fact]
    public void Document_without_frontmatter_returns_full_body()
    {
        var (fm, body) = FrontmatterParser.Parse("# Just markdown\n\nNo fences here.");

        fm.Title.Should().BeNull();
        body.Should().StartWith("# Just markdown");
    }

    [Fact]
    public void Unclosed_fence_is_treated_as_body()
    {
        var (fm, body) = FrontmatterParser.Parse("---\ntitle: broken");

        fm.Title.Should().BeNull();
        body.Should().Contain("title: broken");
    }

    [Fact]
    public void Unknown_frontmatter_keys_are_ignored()
    {
        var (fm, _) = FrontmatterParser.Parse("---\ntitle: ok\nnotAField: whatever\n---\nbody");
        fm.Title.Should().Be("ok");
    }
}
