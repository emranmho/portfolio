using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Portfolio.Api.Auth;

public sealed record JwtDemoOptions(string Issuer, string Audience, SymmetricSecurityKey Key)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public static JwtDemoOptions From(IConfiguration config, IHostEnvironment env)
    {
        // Production key comes from the JWT_DEMO_KEY env var (set in Dokploy);
        // Jwt:DemoKey is the local-dev fallback from appsettings.Development.json.
        var key = config["JWT_DEMO_KEY"] ?? config["Jwt:DemoKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            if (env.IsProduction())
                throw new InvalidOperationException("JWT_DEMO_KEY is required in production.");
            key = "local-dev-demo-signing-key-not-a-secret-0123456789";
        }

        return new JwtDemoOptions(
            config["Jwt:Issuer"] ?? "https://api.emran.blog",
            config["Jwt:Audience"] ?? "https://emran.blog",
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)));
    }
}
