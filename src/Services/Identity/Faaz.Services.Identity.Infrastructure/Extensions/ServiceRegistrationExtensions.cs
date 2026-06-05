using Faaz.BuildingBlocks.Extensions;
using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.Infrastructure.Interfaces.Users;
using Faaz.Services.Identity.Infrastructure.Managers.Auth;
using Faaz.Services.Identity.Infrastructure.Managers.Users;
using Faaz.Services.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Cryptography;

namespace Faaz.Services.Identity.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration config, Assembly webHostAssembly, IWebHostEnvironment env)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<IdentityDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequireLowercase = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequiredLength = 8;
            opts.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        AddJwtAuthentication(services, config);

        services.AddMediatrWithBehaviors(webHostAssembly);

        services.AddScoped<IRefreshTokenServices, RefreshTokenManager>();
        services.AddScoped<IPasswordResetTokenServices, PasswordResetTokenManager>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddSingleton<ITokenService, TokenService>();
        if (env.IsDevelopment())
            services.AddScoped<IEmailService, DevSmtpEmailService>();
        else
            services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddHttpContextAccessor();

        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Faaz Identity API", Version = "v1" });
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

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration config)
    {
        var pem = config["Jwt:PrivateKeyPem"];
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

        // AddIdentity sets cookie as the default challenge scheme — override it so
        // unauthenticated API requests get 401 instead of a redirect to /Account/Login.
        services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opts.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();
    }
}
