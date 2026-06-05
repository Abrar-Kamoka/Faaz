using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Extensions;
using Faaz.Services.Identity.WebHost.DevEmail;
using Faaz.Services.Identity.WebHost.Extensions;
using Faaz.Services.Identity.WebHost.Seeding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

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
    builder.Services.AddIdentityInfrastructure(builder.Configuration, typeof(Program).Assembly, builder.Environment);
    builder.Services.AddIdentityHttpClients(builder.Configuration, builder.Environment);
    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, x =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In-memory bus: consume events in-process so emails reach MailHog/console without RabbitMQ.
            x.AddConsumer<StudentRegisteredDevConsumer>();
            x.AddConsumer<StudentProfileCreatorDevConsumer>();
            x.AddConsumer<SendVerificationEmailDevConsumer>();
            x.AddConsumer<SendPasswordResetEmailDevConsumer>();
            x.AddConsumer<ConsultantEmailVerifiedDevConsumer>();
            x.AddConsumer<ConsultantApprovedDevConsumer>();
            x.AddConsumer<ConsultantRejectedDevConsumer>();
            x.AddConsumer<ConsultantRevisionRequestedDevConsumer>();
        }
    });

    // Rate limiting — keyed by remote IP to slow brute-force and enumeration attacks.
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Sensitive auth endpoints: max 10 requests / minute / IP.
        opts.AddPolicy("auth", ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    Window           = TimeSpan.FromMinutes(1),
                    PermitLimit      = 10,
                    QueueLimit       = 0,
                    AutoReplenishment = true
                }));
    });

    var app = builder.Build();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.Migrate();
        var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
        await AdminSeeder.SeedAsync(scope.ServiceProvider, seederLogger);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "DB migration or seeding skipped — run migrations manually before using data endpoints");
    }

    app.UseFaazMiddleware();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Faaz Identity API v1"));
        app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
    }

    app.MapControllers();

    // Minimal OIDC discovery — lets other services resolve the JWKS URI via Authority
    app.MapGet("/.well-known/openid-configuration", (IConfiguration cfg) =>
    {
        var baseUrl = cfg["Identity:PublicUrl"] ?? "https://localhost:55130";
        var issuer = cfg["Jwt:Issuer"] ?? "faaz-identity";
        return Results.Ok(new
        {
            issuer,
            jwks_uri = $"{baseUrl}/api/v1/auth/.well-known/jwks.json"
        });
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Identity service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
