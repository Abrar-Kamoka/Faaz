using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Faaz.BuildingBlocks.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddFaazRabbitMq(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env,
        Action<IBusRegistrationConfigurator> registerConsumers)
    {
        services.AddMassTransit(x =>
        {
            registerConsumers(x);

            // Always a real broker, dev included. In-memory transport is per-process only — every
            // integration event in this system crosses a service boundary by definition (that's
            // the point of publishing one), so anything less than a real broker silently drops
            // cross-service events: bookings never leave SlotReserved, consultants never see
            // requests, no emails ever send. Dev config points RabbitMq:Host at a locally-installed
            // RabbitMQ (no Docker required) with the same credentials production uses.
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(config["RabbitMq:Host"], config["RabbitMq:VHost"], h =>
                {
                    h.Username(config["RabbitMq:Username"]!);
                    h.Password(config["RabbitMq:Password"]!);
                });

                cfg.UseMessageRetry(r =>
                    r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
