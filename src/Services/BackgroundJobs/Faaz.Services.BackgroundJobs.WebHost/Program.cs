using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Booking.Infrastructure.Extensions;
using Faaz.Services.Booking.WebHost.Jobs;
using Hangfire;
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

    // AddBookingInfrastructure registers Swagger generation, which needs the API explorer
    // even though this worker maps no controllers of its own.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-backgroundjobs");

    // Reuse Booking's infrastructure registrations (DB context, managers, Hangfire, slot locking, HTTP clients).
    builder.Services.AddBookingInfrastructure(builder.Configuration, typeof(Program).Assembly, builder.Environment);
    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, _ => { });

    // Register the same job implementations so Hangfire can resolve them by FQN.
    builder.Services.AddScoped<ICreateSessionRoomJob, CreateSessionRoomJob>();
    builder.Services.AddScoped<INoShowCheckJob, NoShowCheckJob>();
    builder.Services.AddScoped<IForceCloseRoomJob, ForceCloseRoomJob>();
    builder.Services.AddScoped<IReconnectionWindowExpiredJob, ReconnectionWindowExpiredJob>();
    builder.Services.AddScoped<IExpireUnconfirmedBookingsJob, ExpireUnconfirmedBookingsJob>();
    builder.Services.AddScoped<ICleanupExpiredSlotsJob, CleanupExpiredSlotsJob>();
    builder.Services.AddScoped<ISendSessionReminderJob, SendSessionReminderJob>();
    builder.Services.AddScoped<IReleasePendingPayoutsJob, ReleasePendingPayoutsJob>();

    var app = builder.Build();

    app.UseFaazMiddleware();

    if (app.Environment.IsDevelopment())
    {
        // No explicit Authorization filter — Hangfire's own default (LocalRequestsOnlyAuthorizationFilter)
        // applies, restricting the dashboard to loopback requests only.
        app.UseHangfireDashboard("/hangfire");
        app.MapGet("/", () => Results.Redirect("/hangfire")).AllowAnonymous();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "BackgroundJobs service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
