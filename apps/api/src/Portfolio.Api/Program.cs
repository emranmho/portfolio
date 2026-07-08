using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Portfolio.Api.Auth;
using Portfolio.Api.Endpoints;
using Portfolio.Api.Middleware;
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
        if (!path.StartsWithSegments("/api") || path.StartsWithSegments("/api/health"))
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
     .WithExposedHeaders("Retry-After", "X-RateLimit-Limit", "X-RateLimit-Window")));

var app = builder.Build();

// --- Pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints
app.MapHealthEndpoints();
app.MapWhoamiEndpoints();
app.MapProjectEndpoints();
app.MapArticleEndpoints();
app.MapMetricsEndpoints();
app.MapAuthEndpoints();

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
}

app.Run();

public partial class Program;
