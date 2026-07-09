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

public sealed class ProjectFrontmatter
{
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public List<string>? Stack { get; set; }
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }
    public bool Featured { get; set; }
    public int Order { get; set; }
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
    public static (T Frontmatter, string Body) Parse<T>(string markdown) where T : new()
    {
        var text = markdown.ReplaceLineEndings("\n");

        if (!text.StartsWith("---\n", StringComparison.Ordinal))
            return (new T(), text);

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
            return (new T(), text);

        var yaml = text[4..end];
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? "" : text[(bodyStart + 1)..];

        var frontmatter = Deserializer.Deserialize<T>(yaml) ?? new T();
        return (frontmatter, body);
    }
}
