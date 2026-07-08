namespace Portfolio.Application.Abstractions;

public interface IWhoamiReader
{
    /// <summary>Raw JSON payload of content/whoami.json.</summary>
    Task<string> GetJsonAsync(CancellationToken ct);
}
