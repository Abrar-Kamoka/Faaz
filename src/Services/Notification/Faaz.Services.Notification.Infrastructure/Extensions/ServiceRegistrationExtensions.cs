using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.Infrastructure.Managers;
using Faaz.Services.Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Faaz.Services.Notification.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env)
    {
        // Every other service registers this to auto-stamp CreatedAt/UpdatedAt/CreatedBy on save —
        // this one never did, so every NotificationLog row has CreatedAt = NULL. That's what turns
        // into "56 years ago" in the notification drawer (new Date(null) parses as epoch).
        // AuditableEntityInterceptor needs IHttpContextAccessor (to stamp CreatedBy/UpdatedBy from
        // the request) — every other service registering the interceptor also registers this right
        // alongside it; without it, resolving the interceptor throws at startup and the whole host
        // crashes before Kestrel ever binds a port.
        services.AddHttpContextAccessor();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<NotificationDbContext>((sp, options) =>
            options.UseSqlServer(config.GetConnectionString("FaazDb"))
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<INotificationLogServices, NotificationLogManager>();
        services.AddScoped<IEmailSenderService, SmtpEmailSenderService>();
        services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();

        services.AddHttpClient<INotificationIdentityClient, NotificationIdentityClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:IdentityServiceUrl"] ?? "https://localhost:55130");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        return services;
    }
}
