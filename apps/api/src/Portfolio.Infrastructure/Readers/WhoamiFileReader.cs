using System.Text.Json;
using Portfolio.Application.Abstractions;

namespace Portfolio.Infrastructure.Readers;

/// <summary>Serves content/whoami.json verbatim (validated once, cached for process lifetime).</summary>
public sealed class WhoamiFileReader(string contentRoot) : IWhoamiReader
{
    private string? _cached;

    public async Task<string> GetJsonAsync(CancellationToken ct)
    {
        if (_cached is not null)
            return _cached;

        var path = Path.Combine(contentRoot, "whoami.json");
        var json = await File.ReadAllTextAsync(path, ct);
        using var _ = JsonDocument.Parse(json); // fail fast on malformed content
        _cached = json;
        return json;
    }
}
