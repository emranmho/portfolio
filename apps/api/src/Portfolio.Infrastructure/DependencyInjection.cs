using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Abstractions;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Metrics;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Readers;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string appContentRoot)
    {
        var connectionString = configuration.GetConnectionString("Db") ?? "Data Source=portfolio.db";
        services.AddDbContext<PortfolioDbContext>(o => o.UseSqlite(connectionString));

        var contentRoot = ContentRootLocator.Resolve(configuration["Content:Root"], appContentRoot);

        services.AddScoped<MarkdownContentIngester>();
        services.AddScoped<IProjectReader, EfProjectReader>();
        services.AddScoped<IArticleReader, EfArticleReader>();
        services.AddSingleton<IWhoamiReader>(new WhoamiFileReader(contentRoot));
        services.AddSingleton<IMetricsStore, SqliteMetricsStore>();
        services.AddHostedService<MetricsFlushService>();
        services.AddSingleton(new ContentRootHolder(contentRoot));

        return services;
    }
}

/// <summary>Resolved absolute content directory, injectable where the path is needed.</summary>
public sealed record ContentRootHolder(string Path);
