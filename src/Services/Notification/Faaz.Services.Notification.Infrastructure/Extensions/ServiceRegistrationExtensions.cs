using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.Infrastructure.Managers;
using Faaz.Services.Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Faaz.Services.Notification.Infrastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("FaazDb")));

        services.AddScoped<INotificationLogServices, NotificationLogManager>();
        services.AddScoped<IEmailSenderService, SmtpEmailSenderService>();

        return services;
    }
}
