using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Extensions;
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
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-identity");
    builder.Services.AddIdentityInfrastructure(builder.Configuration, typeof(Program).Assembly, builder.Environment);
    builder.Services.AddIdentityHttpClients(builder.Configuration, builder.Environment);
    // Identity only publishes these events (StudentRegistered, SendVerificationEmail,
    // ConsultantEmailVerified, ConsultantApproved/Rejected/RevisionRequested, ...) — it never
    // consumes its own. Notification, Consultant, and Student services already register real,
    // unconditional consumers for all of them. There used to be a set of "dev" consumers registered
    // here too, from back when Development used an in-memory MassTransit transport that couldn't
    // cross process boundaries (so Identity had to handle its own events locally to get any email
    // at all). AddFaazRabbitMq now always connects to the real broker regardless of environment, so
    // those dev consumers had become pure duplicates of the real ones — every event fired twice
    // (e.g. two verification emails) whenever Identity ran with ASPNETCORE_ENVIRONMENT=Development
    // alongside the other services.
    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, x => { });

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
        var rbacLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RbacSeeder");
        await RbacSeeder.SeedAsync(scope.ServiceProvider, rbacLogger);
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
