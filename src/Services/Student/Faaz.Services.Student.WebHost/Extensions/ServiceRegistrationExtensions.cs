using Faaz.Services.Student.WebHost.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Faaz.Services.Student.WebHost.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddStudentHttpClients(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:IdentityServiceUrl"] ?? "https://localhost:55130");
            client.DefaultRequestHeaders.Add("X-Service-Key", config["ServiceApiKey"]);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // Public endpoint — no X-Service-Key needed, same as a browser calling it via the Gateway.
        services.AddHttpClient<IConsultantServiceClient, ConsultantServiceClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ConsultantServiceUrl"] ?? "https://localhost:55132");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        return services;
    }
}
