using MassTransit;
using Microsoft.EntityFrameworkCore;
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
        => services.AddFaazRabbitMqCore(config, env, registerConsumers, outboxDbContext: null);

    // For services where a consumer/handler does SaveChangesAsync() then Publish() as two separate
    // steps — the outbox makes that atomic, but only for the DbContext given here. Callers must also
    // call Publish() BEFORE the SaveChangesAsync() that should carry it: the EF outbox intercepts
    // IPublishEndpoint.Publish and stages the message on that DbContext's change tracker, so it's
    // only captured by a SaveChangesAsync() call that happens after Publish() was invoked, not before.
    public static IServiceCollection AddFaazRabbitMqWithOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env,
        Action<IBusRegistrationConfigurator> registerConsumers)
        where TDbContext : DbContext
        => services.AddFaazRabbitMqCore(config, env, registerConsumers, outboxDbContext: x =>
            x.AddEntityFrameworkOutbox<TDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
                // Default is 10s — lowered so a captured event doesn't sit un-published for that long
                // on user-facing flows (e.g. booking confirmation) when the bus is healthy.
                o.QueryDelay = TimeSpan.FromSeconds(1);
            }));

    private static IServiceCollection AddFaazRabbitMqCore(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env,
        Action<IBusRegistrationConfigurator> registerConsumers,
        Action<IBusRegistrationConfigurator>? outboxDbContext)
    {
        services.AddMassTransit(x =>
        {
            outboxDbContext?.Invoke(x);

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

                // Level 1 — fast in-memory retries for transient blips (DB timeout, momentary broker hiccup).
                cfg.UseMessageRetry(r =>
                    r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                // Level 2 — once Level 1 is exhausted, redeliver via the queue instead of dead-lettering
                // immediately. Covers "downstream service restarting" or "Stripe API down for a minute" —
                // without this, a transient outage longer than ~3s permanently dead-letters the message.
                // REQUIRES the RabbitMQ delayed-exchange plugin (rabbitmq_delayed_message_exchange) enabled
                // on every broker this connects to — the queue-based, plugin-free variant
                // (UseQueueBasedDelayedRedelivery) isn't available until MassTransit 9; we're pinned to 8.3.3.
                cfg.UseDelayedRedelivery(r =>
                    r.Intervals(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10),
                        TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
