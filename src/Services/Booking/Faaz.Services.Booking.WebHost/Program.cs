using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Extensions;
using Faaz.Services.Booking.WebHost.Consumers;
using Faaz.Services.Booking.WebHost.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341"));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-booking");
    builder.Services.AddBookingInfrastructure(builder.Configuration, typeof(Program).Assembly, builder.Environment);
    builder.Services.AddFaazRabbitMqWithOutbox<BookingDbContext>(builder.Configuration, builder.Environment, x =>
    {
        x.AddConsumer<PaymentAuthorizedConsumer>();
        x.AddConsumer<PaymentCapturedConsumer>();
        x.AddConsumer<PaymentFailedConsumer>();
    });

    builder.Services.AddScoped<ICreateSessionRoomJob, CreateSessionRoomJob>();
    builder.Services.AddScoped<INoShowCheckJob, NoShowCheckJob>();
    builder.Services.AddScoped<IForceCloseRoomJob, ForceCloseRoomJob>();
    builder.Services.AddScoped<IReconnectionWindowExpiredJob, ReconnectionWindowExpiredJob>();
    builder.Services.AddScoped<IExpireUnconfirmedBookingsJob, ExpireUnconfirmedBookingsJob>();
    builder.Services.AddScoped<ICleanupExpiredSlotsJob, CleanupExpiredSlotsJob>();
    builder.Services.AddScoped<ISendSessionReminderJob, SendSessionReminderJob>();
    builder.Services.AddScoped<IReleasePendingPayoutsJob, ReleasePendingPayoutsJob>();

    var app = builder.Build();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "DB migration skipped — run migrations manually before using data endpoints");
    }

    app.UseStaticFiles();
    app.UseFaazMiddleware();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    // No explicit Authorization filter — Hangfire's own default (LocalRequestsOnlyAuthorizationFilter)
    // applies, restricting the dashboard to loopback requests only. This dashboard exposes and can
    // trigger jobs that touch payouts and refunds, so it must never be open to arbitrary callers —
    // remote access for ops should go through an SSH tunnel/VPN to loopback, not a public route.
    app.UseHangfireDashboard("/hangfire");

    RecurringJob.AddOrUpdate<IExpireUnconfirmedBookingsJob>(
        "expire-unconfirmed-bookings",
        "maintenance",
        j => j.ExecuteAsync(),
        "*/15 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    RecurringJob.AddOrUpdate<IReleasePendingPayoutsJob>(
        "release-pending-payouts",
        "maintenance",
        j => j.ExecuteAsync(),
        "0 2 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    RecurringJob.AddOrUpdate<ICleanupExpiredSlotsJob>(
        "cleanup-expired-slots",
        "maintenance",
        j => j.ExecuteAsync(),
        "*/5 * * * *", // 5 min — the slot hold itself is only 10 min; hourly left it stale far too long
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Faaz Booking API v1"));
        app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Booking service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
