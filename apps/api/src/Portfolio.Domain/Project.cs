namespace Portfolio.Domain;

public sealed class Project
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string Summary { get; set; }
    public string Description { get; set; } = "";
    public string DescriptionHtml { get; set; } = "";
    public List<string> Stack { get; set; } = [];
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }
    public bool Featured { get; set; }
    public int SortOrder { get; set; }
}
