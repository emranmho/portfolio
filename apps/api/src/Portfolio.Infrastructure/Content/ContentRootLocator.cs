namespace Portfolio.Infrastructure.Content;

public static class ContentRootLocator
{
    /// <summary>
    /// Resolves the content directory. Order: configured path (absolute or relative to
    /// <paramref name="appContentRoot"/>), then walking up from the app base directory
    /// looking for a "content/whoami.json" (covers `dotnet run` from anywhere in the repo).
    /// </summary>
    public static string Resolve(string? configuredPath, string appContentRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var candidate = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(appContentRoot, configuredPath));
            if (Directory.Exists(candidate))
                return candidate;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "whoami.json")))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Content directory not found. Configured path: '{configuredPath}' (relative to '{appContentRoot}'). " +
            "Set Content:Root in configuration or the Content__Root environment variable.");
    }
}
