using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Auth;

namespace Portfolio.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/token", (JwtDemoOptions jwt) =>
            {
                var now = DateTime.UtcNow;
                var expires = now.Add(JwtDemoOptions.Lifetime);
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, $"guest-{RandomNumberGenerator.GetHexString(8, lowercase: true)}"),
                    new Claim("role", "explorer"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                };

                var token = new JwtSecurityToken(
                    issuer: jwt.Issuer,
                    audience: jwt.Audience,
                    claims: claims,
                    notBefore: now,
                    expires: expires,
                    signingCredentials: new SigningCredentials(jwt.Key, SecurityAlgorithms.HmacSha256));

                return Results.Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    tokenType = "Bearer",
                    expiresAtUtc = expires,
                    expiresInSeconds = (int)JwtDemoOptions.Lifetime.TotalSeconds,
                    note = "Demo token — no credentials required, nothing real is protected. Try GET /api/secret with and without it.",
                });
            })
            .WithName("IssueDemoToken")
            .WithTags("Auth demo")
            .WithSummary("Issues a short-lived (15 min) demo JWT — no credentials required")
            .WithDescription("A teaching toy, not security: content writes happen via git. Use the token as `Authorization: Bearer <token>` against /api/secret.");

        app.MapGet("/api/secret", (ClaimsPrincipal user) =>
            {
                var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
                return Results.Ok(new
                {
                    message = "You found the secret. There was nothing to protect — but now you know how a JWT carries identity.",
                    subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub),
                    claims,
                });
            })
            .RequireAuthorization()
            .WithName("GetSecret")
            .WithTags("Auth demo")
            .WithSummary("JWT-protected; echoes your decoded claims")
            .WithDescription("Returns 401 without a token — that's the demo. Get one from POST /api/auth/token.");

        return app;
    }
}
