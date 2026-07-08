using System.Text.RegularExpressions;

namespace Portfolio.Domain;

/// <summary>
/// URL-safe identifier: lowercase kebab-case, e.g. "how-this-portfolio-works".
/// </summary>
public readonly partial record struct Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!SlugPattern().IsMatch(normalized))
            throw new ArgumentException(
                $"'{value}' is not a valid slug. Expected lowercase kebab-case (a-z, 0-9, single hyphens).",
                nameof(value));

        return new Slug(normalized);
    }

    public static bool TryCreate(string? value, out Slug slug)
    {
        slug = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().ToLowerInvariant();
        if (!SlugPattern().IsMatch(normalized))
            return false;

        slug = new Slug(normalized);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
