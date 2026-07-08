using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Search;

/// <summary>
/// Full-text search over articles and projects via SQLite FTS5 — deliberately
/// not Elasticsearch: one file, zero infrastructure, and bm25 ranking is more
/// than this corpus needs. The index is rebuilt from scratch after every content
/// ingest; at this size a rebuild is milliseconds.
/// </summary>
public sealed class SqliteFtsSearchService(PortfolioDbContext db) : ISearchService
{
    private const int MaxResults = 20;
    private const int MaxTerms = 8;

    public async Task RebuildIndexAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE VIRTUAL TABLE IF NOT EXISTS search_index
                USING fts5(type UNINDEXED, slug UNINDEXED, title, body, tokenize='porter unicode61');
            DELETE FROM search_index;
            INSERT INTO search_index(type, slug, title, body)
                SELECT 'article', "Slug", "Title", "Summary" || ' ' || "RawMarkdown" FROM "Articles";
            INSERT INTO search_index(type, slug, title, body)
                SELECT 'project', "Slug", "Name", "Summary" || ' ' || "Description" FROM "Projects";
            """, ct);
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, CancellationToken ct)
    {
        var match = ToMatchExpression(query);
        if (match.Length == 0)
            return [];

        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            // bm25 rank ascending = most relevant first; snippet() highlights the
            // best-matching stretch of the body column (index 3).
            cmd.CommandText =
                """
                SELECT type, slug, title, snippet(search_index, 3, '<mark>', '</mark>', ' … ', 12)
                FROM search_index
                WHERE search_index MATCH $match
                ORDER BY rank
                LIMIT $limit
                """;
            AddParameter(cmd, "$match", match);
            AddParameter(cmd, "$limit", MaxResults);

            var results = new List<SearchResultDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new SearchResultDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }
            return results;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// FTS5 MATCH has its own query syntax (AND, OR, NEAR, parens) that 500s on
    /// malformed input. Quoting every term neutralizes the operators; the trailing
    /// * makes the last-typed word match as a prefix, so "sqli" finds "sqlite".
    /// </summary>
    internal static string ToMatchExpression(string query)
    {
        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(MaxTerms)
            .Select(t => $"\"{t.Replace("\"", "\"\"")}\"*");
        return string.Join(" ", terms);
    }

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
