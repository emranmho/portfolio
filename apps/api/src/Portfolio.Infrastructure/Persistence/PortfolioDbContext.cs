using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Portfolio.Domain;

namespace Portfolio.Infrastructure.Persistence;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<MetricBucket> MetricBuckets => Set<MetricBucket>();
    public DbSet<Deploy> Deploys => Set<Deploy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Stack)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();
            e.Property(a => a.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<MetricBucket>(e =>
        {
            e.HasIndex(b => new { b.BucketStartUtc, b.Route }).IsUnique();
        });

        modelBuilder.Entity<Deploy>(e =>
        {
            e.HasIndex(d => d.DeployedAtUtc);
        });
    }
}
