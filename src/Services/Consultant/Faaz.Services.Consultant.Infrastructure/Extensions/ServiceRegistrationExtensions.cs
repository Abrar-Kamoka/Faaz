using Faaz.BuildingBlocks.Extensions;
using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Consultant.Infrastructure.DatabaseContext;
using Faaz.Services.Consultant.Infrastructure.HttpClients;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.Infrastructure.Managers;
using Faaz.Services.Consultant.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;

namespace Faaz.Services.Consultant.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddConsultantInfrastructure(this IServiceCollection services, IConfiguration config, Assembly webHostAssembly, IHostEnvironment env)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<ConsultantDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        AddJwtAuthentication(services, config, env);

        services.AddMediatrWithBehaviors(webHostAssembly);

        services.AddScoped<IConsultantApplicationServices, ConsultantApplicationManager>();
        services.AddScoped<IConsultantProfileServices, ConsultantProfileManager>();
        services.AddScoped<IConsultantCredentialServices, ConsultantCredentialManager>();

        services.AddHttpContextAccessor();

        services.AddHttpClient<IBookingReviewClient, BookingReviewClient>(client =>
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

        services.AddHttpClient<IAdministrationReferenceClient, AdministrationReferenceClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:AdministrationServiceUrl"] ?? "https://localhost:55136");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Faaz Consultant API", Version = "v1" });
            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT access token"
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

            var xmlFile = $"{webHostAssembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                opts.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var jwksUrl = config["Identity:JwksUrl"];
        if (string.IsNullOrWhiteSpace(jwksUrl))
        {
            var pem = config["Jwt:PublicKeyPem"];
            if (string.IsNullOrWhiteSpace(pem)) return;

            var rsa = RSA.Create();
            rsa.ImportFromPem(pem.Replace("\\n", "\n"));
            var publicKey = new RsaSecurityKey(rsa);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    opts.MapInboundClaims = false;
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = config["Jwt:Issuer"] ?? "faaz-identity",
                        ValidateAudience = true,
                        ValidAudience = config["Jwt:Audience"] ?? "faaz-api",
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = publicKey,
                        RoleClaimType = "role"
                    };
                });
        }
        else
        {
            // opts.Authority is intentionally NOT used here — it triggers OIDC discovery at
            // /.well-known/openid-configuration but Identity only exposes a raw JWKS endpoint.
            // UseSecurityTokenValidators = true forces the classic JwtSecurityTokenHandler path
            // which correctly invokes IssuerSigningKeyResolver; the default JsonWebTokenHandler
            // in .NET 7/8 has a different validation flow that bypasses it.
            // Keys are cached for 24 h and force-refreshed when the token's kid is not found.
            var isDev = env.IsDevelopment();
            SecurityKey[]? jwksCache = null;
            DateTime jwksCacheExpiry  = DateTime.MinValue;
            var jwksCacheLock         = new object();

            SecurityKey[] FetchKeys()
            {
                var handler = new HttpClientHandler();
                if (isDev)
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using var client = new HttpClient(handler);
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
                    jwksCache      = FetchKeys();
                    jwksCacheExpiry = DateTime.UtcNow.AddHours(24);
                    return jwksCache;
                }
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
#pragma warning disable CS0618
                    opts.UseSecurityTokenValidators = true; // use JwtSecurityTokenHandler, not JsonWebTokenHandler
#pragma warning restore CS0618
                    opts.MapInboundClaims = false;
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = config["Jwt:Issuer"] ?? "faaz-identity",
                        ValidateAudience = true,
                        ValidAudience = config["Jwt:Audience"] ?? "faaz-api",
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RoleClaimType = "role",
                        IssuerSigningKeyResolver = (_, _, kid, _) =>
                        {
                            var keys = ResolveKeys();
                            if (!string.IsNullOrEmpty(kid) && !keys.Any(k => k.KeyId == kid))
                                keys = ResolveKeys(forceRefresh: true);
                            return keys;
                        }
                    };
                });
        }

        services.AddAuthorization(opts =>
        {
            // Consultant is filling their profile (SettingUpProfile=2) or already Active (Active=4). Admin (role=3) always allowed.
            opts.AddPolicy("ConsultantSetupOrActive", policy =>
                policy.RequireAssertion(ctx =>
                {
                    var role = ctx.User.FindFirst("role")?.Value;
                    if (role == "3") return true; // Admin
                    if (role != "2") return false; // Not a Consultant
                    var status = ctx.User.FindFirst("consultant_status")?.Value;
                    return status == "2" || status == "4"; // SettingUpProfile or Active
                }));

            // Consultant is fully Active (Active=4). Admin (role=3) always allowed.
            opts.AddPolicy("ActiveConsultant", policy =>
                policy.RequireAssertion(ctx =>
                {
                    var role = ctx.User.FindFirst("role")?.Value;
                    if (role == "3") return true; // Admin
                    if (role != "2") return false;
                    var status = ctx.User.FindFirst("consultant_status")?.Value;
                    return status == "4"; // Active only
                }));
        });
    }
}
