using Faaz.Services.Identity.WebHost.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Faaz.Services.Identity.WebHost.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddIdentityHttpClients(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddHttpClient<IStudentServiceClient, StudentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:StudentServiceUrl"] ?? "http://localhost:5102");
            client.DefaultRequestHeaders.Add("X-Service-Key", config["ServiceApiKey"]);
        })
        .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(env))
        .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

        services.AddHttpClient<IConsultantServiceClient, ConsultantServiceClient>(client =>
        {
            client.BaseAddress = new Uri(config["Services:ConsultantServiceUrl"] ?? "http://localhost:5103");
            client.DefaultRequestHeaders.Add("X-Service-Key", config["ServiceApiKey"]);
        })
        .ConfigurePrimaryHttpMessageHandler(() => BuildHandler(env))
        .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

        return services;
    }

    private static HttpClientHandler BuildHandler(IHostEnvironment env)
    {
        var handler = new HttpClientHandler();
        if (env.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    }
}
