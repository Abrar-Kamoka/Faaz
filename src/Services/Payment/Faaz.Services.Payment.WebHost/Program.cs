using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Extensions;
using Faaz.Services.Payment.WebHost.Consumers;
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
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-payment");
    builder.Services.AddPaymentInfrastructure(builder.Configuration, typeof(Program).Assembly, builder.Environment);
    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, x =>
    {
        x.AddConsumer<BookingConfirmedConsumer>();
        x.AddConsumer<BookingCancelledConsumer>();
        x.AddConsumer<SessionCompletedConsumer>();
        x.AddConsumer<SessionNoShowConsumer>();
        x.AddConsumer<RefundAppealApprovedConsumer>();
        x.AddConsumer<DisputeResolvedConsumer>();
        x.AddConsumer<PayoutReleasedConsumer>();
    });

    var app = builder.Build();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Faaz Payment API v1"));
        app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Payment service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
