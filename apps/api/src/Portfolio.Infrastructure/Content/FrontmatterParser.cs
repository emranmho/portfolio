using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Portfolio.Infrastructure.Content;

public sealed class ArticleFrontmatter
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public DateTime? Date { get; set; }
    public List<string>? Tags { get; set; }
}

public static class FrontmatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Splits a markdown document into YAML frontmatter (between leading "---" fences)
    /// and the markdown body. Returns an empty frontmatter when no fence is present.
    /// </summary>
    public static (ArticleFrontmatter Frontmatter, string Body) Parse(string markdown)
    {
        var text = markdown.ReplaceLineEndings("\n");

        if (!text.StartsWith("---\n", StringComparison.Ordinal))
            return (new ArticleFrontmatter(), text);

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
            return (new ArticleFrontmatter(), text);

        var yaml = text[4..end];
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? "" : text[(bodyStart + 1)..];

        var frontmatter = Deserializer.Deserialize<ArticleFrontmatter>(yaml) ?? new ArticleFrontmatter();
        return (frontmatter, body);
    }
}
