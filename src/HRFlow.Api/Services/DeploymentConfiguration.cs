using System.Net;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

namespace HRFlow.Api.Services;

/// <summary>Rejects unsafe host configuration before services, migrations or listeners can start.</summary>
public static class DeploymentConfiguration
{
    /// <summary>Checks secrets without echoing values; origin lists remain optional for same-origin hosting.</summary>
    public static void Validate(WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var development = builder.Environment.IsDevelopment();
        foreach (var key in new[] { "Issuer", "Audience" })
            if (string.IsNullOrWhiteSpace(config[$"Authentication:Jwt:{key}"]))
                throw new InvalidOperationException($"Configure Authentication:Jwt:{key}.");
        foreach (var key in new[] { "SigningKey", "RefreshTokenPepper" })
        {
            var value = config[$"Authentication:Jwt:{key}"] ?? "";
            if (Encoding.UTF8.GetByteCount(value) < 32 || value.Distinct().Count() < 8
                || !development && (value.Contains('<') || value.Contains('>')
                    || new[] { "demo", "example", "default", "changeme", "change-me", "change_me", "your-secret", "yoursupersecret", "development", "placeholder" }
                        .Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException($"Configure Authentication:Jwt:{key} with independently generated secret material of at least 32 bytes; do not use defaults.");
        }
        if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use explicit Deployment proxy trust; automatic forwarded-header trust is unsupported.");
        if (!development)
        {
            ValidateOrigin(config["Activation:ApplicationOrigin"], false);
            if (config.GetSection("Seeding").GetChildren().Any())
                throw new InvalidOperationException("Remove Development Seeding configuration from this environment.");
            if (!string.IsNullOrEmpty(config["Activation:Delivery"]))
                throw new InvalidOperationException("Production invitation delivery is unavailable; remove Activation:Delivery. Development pickup is forbidden.");
            var hosts = config["AllowedHosts"];
            if (string.IsNullOrWhiteSpace(hosts) || hosts.Contains('*'))
                throw new InvalidOperationException("Configure explicit AllowedHosts without wildcards.");
        }
        var origins = config.GetSection("Deployment:CorsOrigins").Get<string[]>()
            ?? (development ? ["http://localhost:5173", "http://127.0.0.1:5173"] : []);
        foreach (var origin in origins) ValidateOrigin(origin, development);
        builder.Services.AddCors(options => options.AddPolicy("ConfiguredOrigins", policy =>
        {
            if (origins.Length != 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }));
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.KnownNetworks.Clear(); options.KnownProxies.Clear();
            options.ForwardLimit = 1;
            if (!config.GetValue<bool>("Deployment:ProxyEnabled")) return;
            var proxies = config.GetSection("Deployment:KnownProxies").Get<string[]>() ?? [];
            if (proxies.Length == 0) throw new InvalidOperationException("Configure explicit Deployment:KnownProxies.");
            foreach (var proxy in proxies)
            {
                if (!IPAddress.TryParse(proxy, out var address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
                    throw new InvalidOperationException("Deployment:KnownProxies must contain individual trusted IP addresses.");
                options.KnownProxies.Add(address);
            }
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        // Resolve options now rather than discovering invalid proxy settings on the first request.
        if (config.GetValue<bool>("Deployment:ProxyEnabled"))
        {
            var proxies = config.GetSection("Deployment:KnownProxies").Get<string[]>() ?? [];
            if (proxies.Length == 0 || proxies.Any(p => !IPAddress.TryParse(p, out var ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)))
                throw new InvalidOperationException("Configure individual trusted Deployment:KnownProxies IP addresses.");
        }
    }

    private static void ValidateOrigin(string? value, bool development)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0
            || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || !(uri.Scheme == "https" || development && uri.Scheme == "http" && uri.IsLoopback)
            || value!.EndsWith('/'))
            throw new InvalidOperationException("Configure trusted application/CORS origins as HTTPS scheme and host with optional port, without paths or trailing slash (Development loopback HTTP is allowed).");
    }
}
