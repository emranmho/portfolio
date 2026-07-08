namespace Portfolio.Application.Abstractions;

public interface IResumeReader
{
    /// <summary>Raw JSON payload of content/resume.json.</summary>
    Task<string> GetJsonAsync(CancellationToken ct);
}
