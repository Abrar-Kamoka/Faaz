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

            if (env.IsDevelopment())
            {
                // No RabbitMQ needed in dev — in-memory bus, consumers run in-process.
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
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
            }
        });

        return services;
    }
}
