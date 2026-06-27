using Faaz.BuildingBlocks.Extensions;
using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Faaz.Services.Payment.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        Assembly webHostAssembly,
        IHostEnvironment env)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<PaymentDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        AddJwtAuthentication(services, config, env);

        services.AddMediatrWithBehaviors(webHostAssembly);

        services.Scan(scan => scan
            .FromAssemblyOf<PaymentDbContext>()
            .AddClasses(c => c.InNamespaces("Faaz.Services.Payment.Infrastructure.Managers"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<IPaymentGateway, StripePaymentGateway>();

        services.AddHttpClient<IPaymentConsultantClient, PaymentConsultantClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ConsultantServiceUrl"] ?? "https://localhost:55132");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        services.AddHttpClient<IBookingClient, BookingClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:BookingServiceUrl"] ?? "https://localhost:55134");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        services.AddHttpContextAccessor();
        AddSwagger(services, webHostAssembly);

        return services;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var jwksUrl = config["Identity:JwksUrl"];
        if (string.IsNullOrWhiteSpace(jwksUrl)) return;

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
            opts.AddPolicy("StudentOnly", policy =>
                policy.RequireAssertion(ctx => ctx.User.FindFirst("role")?.Value == "1"));

            opts.AddPolicy("ConsultantOnly", policy =>
                policy.RequireAssertion(ctx =>
                {
                    var role   = ctx.User.FindFirst("role")?.Value;
                    var status = ctx.User.FindFirst("consultant_status")?.Value;
                    if (role == "3") return true;
                    return role == "2" && status == "4";
                }));

            opts.AddPolicy("BookingParticipantOrAdmin", policy =>
                policy.RequireAssertion(ctx => ctx.User.FindFirst("role")?.Value is "1" or "2" or "3"));

            opts.AddPolicy("AdminOnly", policy =>
                policy.RequireAssertion(ctx => ctx.User.FindFirst("role")?.Value == "3"));
        });
    }

    private static void AddSwagger(IServiceCollection services, Assembly webHostAssembly)
    {
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Faaz Payment API", Version = "v1" });
            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                Description  = "Enter your JWT access token"
            });
            opts.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });
    }
}
