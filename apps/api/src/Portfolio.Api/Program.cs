using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Portfolio.Api.Auth;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Live;
using Portfolio.Api.Middleware;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Features.Articles.GetArticleBySlug;
using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Content;
using Portfolio.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// --- Logging: structured JSON to stdout; `docker logs` is the log store at this scale.
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext();

    if (ctx.HostingEnvironment.IsDevelopment())
        cfg.WriteTo.Console();
    else
        cfg.WriteTo.Console(new CompactJsonFormatter());
});

// --- Services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetArticleBySlugQuery>());
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddSingleton<LiveRequestFeed>();

// --- Demo JWT (auth as a feature, not security — see README §1.5)
var jwtOptions = JwtDemoOptions.From(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.Key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// --- Rate limiting: fixed window per IP on /api/* (health excluded).
// Single instance ⇒ in-memory is correct; Redis here would be cargo cult.
var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 20);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var path = ctx.Request.Path;
        // /api/live excluded: SSE holds one long-lived request; reconnects shouldn't eat quota.
        if (!path.StartsWithSegments("/api")
            || path.StartsWithSegments("/api/health")
            || path.StartsWithSegments("/api/live"))
            return RateLimitPartition.GetNoLimiter("unlimited");

        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
        });
    });
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
        await Results.Problem(
                title: "Too many requests",
                detail: $"Rate limit is {permitLimit} requests per {windowSeconds}s per IP. Slow down and retry.",
                statusCode: StatusCodes.Status429TooManyRequests)
            .ExecuteAsync(context.HttpContext);
    };
});

// --- CORS: the playground on emran.blog is the only browser consumer.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()
     .WithExposedHeaders("Retry-After", "X-RateLimit-Limit", "X-RateLimit-Window", "X-Api-Version")));

var app = builder.Build();

// --- Pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

// One line per request: method, path, status, elapsed ms. Own logger category
// (not "Microsoft.AspNetCore"), so it survives the Warning-level override above.
// Demotes client-disconnect cancellations (e.g. closing an open SSE stream) from
// Error to Debug — those aren't application failures, just a closed connection.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) => ex switch
    {
        OperationCanceledException => LogEventLevel.Debug,
        not null => LogEventLevel.Error,
        _ when httpContext.Response.StatusCode > 499 => LogEventLevel.Error,
        _ => LogEventLevel.Information,
    };
});

// CORS must run before rate limiting/auth: a browser's CORS preflight (OPTIONS,
// sent whenever a request carries a non-simple header or method) needs to
// succeed unconditionally, or the real request never gets sent.
app.UseCors();

app.UseMiddleware<MetricsMiddleware>();

// Rate-limit headers on every /api response so the playground can display them.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers["X-RateLimit-Limit"] = permitLimit.ToString();
        context.Response.Headers["X-RateLimit-Window"] = $"{windowSeconds}s";
    }
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints
app.MapHealthEndpoints();
app.MapWhoamiEndpoints();
app.MapResumeEndpoints();
app.MapProjectEndpoints();
app.MapArticleEndpoints();
app.MapMetricsEndpoints();
app.MapAuthEndpoints();
app.MapSearchEndpoints();
app.MapLiveFeedEndpoints();
app.MapV2Endpoints();

app.MapOpenApi();
app.MapScalarApiReference("/docs", options => options
    .WithTitle("emran.blog API")
    .WithTheme(ScalarTheme.DeepSpace));

// --- Startup: create schema, ingest content from git, record deploy.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await db.Database.EnsureCreatedAsync();

    var contentRoot = scope.ServiceProvider.GetRequiredService<ContentRootHolder>().Path;
    var ingester = scope.ServiceProvider.GetRequiredService<MarkdownContentIngester>();
    await ingester.IngestAsync(contentRoot);
    await ingester.RecordDeployAsync(app.Configuration["GIT_SHA"] ?? "dev");

    // Content is in the database now — (re)build the FTS5 index from it.
    await scope.ServiceProvider.GetRequiredService<ISearchService>().RebuildIndexAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
