using FluentAssertions;
using Portfolio.Domain;

namespace Portfolio.Tests.Unit;

public class SlugTests
{
    [Theory]
    [InlineData("hello-world")]
    [InlineData("a")]
    [InlineData("dotnet-10-api")]
    public void Create_accepts_valid_kebab_case(string input) =>
        Slug.Create(input).Value.Should().Be(input);

    [Fact]
    public void Create_normalizes_case_and_whitespace() =>
        Slug.Create("  Hello-World ").Value.Should().Be("hello-world");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has spaces")]
    [InlineData("double--hyphen")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("under_score")]
    public void Create_rejects_invalid_input(string input)
    {
        var act = () => Slug.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryCreate_returns_false_instead_of_throwing()
    {
        Slug.TryCreate("not a slug!", out _).Should().BeFalse();
        Slug.TryCreate("fine-slug", out var slug).Should().BeTrue();
        slug.Value.Should().Be("fine-slug");
    }
}
