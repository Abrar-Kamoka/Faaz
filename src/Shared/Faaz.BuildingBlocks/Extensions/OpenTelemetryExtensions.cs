using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Faaz.BuildingBlocks.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddFaazOpenTelemetry(
        this IServiceCollection services,
        IConfiguration config,
        string serviceName)
    {
        var otlpEndpoint = config["OpenTelemetry:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = true;
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }
}
