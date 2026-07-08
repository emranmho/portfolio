namespace Portfolio.Domain;

public sealed class Article
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public DateTime PublishedAtUtc { get; set; }
    public string RawMarkdown { get; set; } = "";
    public string Html { get; set; } = "";
    public int ReadingTimeMinutes { get; set; }
}
