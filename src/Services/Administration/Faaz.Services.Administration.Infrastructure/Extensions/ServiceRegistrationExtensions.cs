using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Faaz.Services.Administration.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAdministrationInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<AdminDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        AddJwtAuthentication(services, config, env);

        services.Scan(scan => scan
            .FromAssemblyOf<AdminDbContext>()
            .AddClasses(c => c.InNamespaces("Faaz.Services.Administration.Infrastructure.Managers"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        var isDev = env.IsDevelopment();

        services.AddHttpClient<IAdminIdentityClient, AdminIdentityClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:IdentityServiceUrl"] ?? "https://localhost:55128");
        }).ConfigurePrimaryHttpMessageHandler(() => BuildHandler(isDev));

        services.AddHttpClient<IAdminConsultantClient, AdminConsultantClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ConsultantServiceUrl"] ?? "https://localhost:55132");
        }).ConfigurePrimaryHttpMessageHandler(() => BuildHandler(isDev));

        services.AddHttpClient<IAdminBookingClient, AdminBookingClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:BookingServiceUrl"] ?? "https://localhost:55134");
        }).ConfigurePrimaryHttpMessageHandler(() => BuildHandler(isDev));

        services.AddHttpClient<IAdminPaymentClient, AdminPaymentClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:PaymentServiceUrl"] ?? "https://localhost:55135");
        }).ConfigurePrimaryHttpMessageHandler(() => BuildHandler(isDev));

        services.AddHttpClient<IAdminNotificationClient, AdminNotificationClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:NotificationServiceUrl"] ?? "https://localhost:55133");
        }).ConfigurePrimaryHttpMessageHandler(() => BuildHandler(isDev));

        services.AddHttpContextAccessor();

        return services;
    }

    private static HttpClientHandler BuildHandler(bool isDev)
    {
        var h = new HttpClientHandler();
        if (isDev)
            h.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return h;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var jwksUrl = config["Identity:JwksUrl"];
        if (string.IsNullOrWhiteSpace(jwksUrl))
            return;

        var isDev         = env.IsDevelopment();
        SecurityKey[]? jwksCache = null;
        DateTime jwksCacheExpiry  = DateTime.MinValue;
        var jwksCacheLock         = new object();

        SecurityKey[] FetchKeys()
        {
            var handler = new HttpClientHandler();
            if (isDev)
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new System.Net.Http.HttpClient(handler);
            var json = client.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
            return new JsonWebKeySet(json).GetSigningKeys().ToArray();
        }

        IEnumerable<SecurityKey> ResolveKeys(bool forceRefresh = false)
        {
            if (!forceRefresh && jwksCache is not null && DateTime.UtcNow < jwksCacheExpiry)
                return jwksCache;
            lock (jwksCacheLock)
            {
                if (!forceRefresh && jwksCache is not null && DateTime.UtcNow < jwksCacheExpiry)
                    return jwksCache;
                jwksCache       = FetchKeys();
                jwksCacheExpiry = DateTime.UtcNow.AddHours(24);
                return jwksCache;
            }
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
#pragma warning disable CS0618
                opts.UseSecurityTokenValidators = true;
#pragma warning restore CS0618
                opts.MapInboundClaims = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = config["Jwt:Issuer"] ?? "faaz-identity",
                    ValidateAudience         = true,
                    ValidAudience            = config["Jwt:Audience"] ?? "faaz-api",
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType            = "role",
                    IssuerSigningKeyResolver = (_, _, kid, _) =>
                    {
                        var keys = ResolveKeys();
                        if (!string.IsNullOrEmpty(kid) && !keys.Any(k => k.KeyId == kid))
                            keys = ResolveKeys(forceRefresh: true);
                        return keys;
                    }
                };
            });

        services.AddAuthorization(opts =>
        {
            opts.AddPolicy("AdminOnly", policy =>
                policy.RequireAssertion(ctx =>
                {
                    var role = ctx.User.FindFirst("role")?.Value;
                    return role == "3";
                }));
        });
    }

}
