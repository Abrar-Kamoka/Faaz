using Faaz.BuildingBlocks.Extensions;
using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Student.Infrastructure.DatabaseContext;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.Infrastructure.Managers;
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

namespace Faaz.Services.Student.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddStudentInfrastructure(this IServiceCollection services, IConfiguration config, Assembly webHostAssembly, IHostEnvironment env)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<StudentDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        AddJwtAuthentication(services, config, env);

        services.AddMediatrWithBehaviors(webHostAssembly);

        services.AddScoped<IStudentProfileServices, StudentProfileManager>();

        services.AddHttpContextAccessor();

        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Faaz Student API", Version = "v1" });
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
            // opts.Authority is intentionally NOT used here. It triggers OIDC discovery
            // (fetching /.well-known/openid-configuration), but the Identity service only
            // exposes a raw JWKS endpoint — not a full OIDC discovery document.
            // Keys are cached for 24 h and force-refreshed if signature validation fails
            // (handles key rotation without a process restart).
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

        services.AddAuthorization();
    }
}
