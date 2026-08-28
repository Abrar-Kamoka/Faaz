using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Extensions;
using Faaz.Services.Notification.WebHost.Consumers;
using Faaz.Services.Notification.WebHost.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-notification");
    builder.Services.AddNotificationInfrastructure(builder.Configuration, builder.Environment);

    // JWT: same dynamic JWKS pattern as Student/Consultant services
    var jwksUrl = builder.Configuration["Identity:JwksUrl"];
    var isDev   = builder.Environment.IsDevelopment();

    if (!string.IsNullOrWhiteSpace(jwksUrl))
    {
        SecurityKey[]? jwksCache     = null;
        DateTime jwksCacheExpiry     = DateTime.MinValue;
        var jwksCacheLock            = new object();

        SecurityKey[] FetchKeys()
        {
            var handler = new HttpClientHandler();
            if (isDev) handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new HttpClient(handler);
            var json = client.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
            return new JsonWebKeySet(json).GetSigningKeys().ToArray();
        }

        IEnumerable<SecurityKey> ResolveKeys(bool forceRefresh = false)
        {
            if (!forceRefresh && jwksCache is not null && DateTime.UtcNow < jwksCacheExpiry) return jwksCache;
            lock (jwksCacheLock)
            {
                if (!forceRefresh && jwksCache is not null && DateTime.UtcNow < jwksCacheExpiry) return jwksCache;
                jwksCache = FetchKeys(); jwksCacheExpiry = DateTime.UtcNow.AddHours(24); return jwksCache;
            }
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
#pragma warning disable CS0618
                opts.UseSecurityTokenValidators = true;
#pragma warning restore CS0618
                opts.MapInboundClaims = false;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = builder.Configuration["Jwt:Issuer"] ?? "faaz-identity",
                    ValidateAudience         = true,
                    ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "faaz-api",
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType            = "role",
                    IssuerSigningKeyResolver = (_, _, kid, _) =>
                    {
                        var keys = ResolveKeys();
                        if (!string.IsNullOrEmpty(kid) && !keys.Any(k => k.KeyId == kid))
                            keys = ResolveKeys(forceRefresh: true);
                        return keys;
                    }
                };
                // Support SignalR token from query string
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.Request.Path.StartsWithSegments("/hubs/notifications"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });
    }

    builder.Services.AddAuthorization();

    var signalR = builder.Services.AddSignalR();
    if (!builder.Environment.IsDevelopment())
        signalR.AddStackExchangeRedis(builder.Configuration["Redis:ConnectionString"]!);

    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, x =>
    {
        // Renamed to *NotificationConsumer wherever another service owns a same-named class for the
        // same event (e.g. Student.Consumers.StudentRegisteredConsumer creates the profile stub;
        // this service's copy sends the welcome/verify email). MassTransit's default endpoint naming
        // is based on the consumer's simple type name, so two identically-named classes in different
        // services silently bind to the SAME queue and become competing consumers — each message
        // goes to only one of them, at random, so the "losing" side never runs. That's why
        // verification emails (and several other notifications below) only fired intermittently.
        // Phase 2
        x.AddConsumer<StudentRegisteredNotificationConsumer>();
        x.AddConsumer<ConsultantEmailVerifiedNotificationConsumer>();
        x.AddConsumer<ConsultantApprovedNotificationConsumer>();
        x.AddConsumer<ConsultantRejectedConsumer>();
        x.AddConsumer<ConsultantRevisionRequestedConsumer>();
        x.AddConsumer<SendVerificationEmailConsumer>();
        x.AddConsumer<SendPasswordResetEmailConsumer>();
        // Phase 3
        x.AddConsumer<BookingRequestReceivedConsumer>();
        x.AddConsumer<BookingRescheduledConsumer>();
        x.AddConsumer<BookingConfirmedNotificationConsumer>();
        x.AddConsumer<BookingCancelledNotificationConsumer>();
        x.AddConsumer<BookingDisputedConsumer>();
        x.AddConsumer<DisputeResolvedNotificationConsumer>();
        x.AddConsumer<SessionReminderConsumer>();
        x.AddConsumer<SessionNoShowNotificationConsumer>();
        x.AddConsumer<PayoutReleasedNotificationConsumer>();
        x.AddConsumer<PayoutFailedConsumer>();
        x.AddConsumer<RefundIssuedConsumer>();
        x.AddConsumer<PaymentCapturedNotificationConsumer>();
        x.AddConsumer<PaymentAuthorizedNotificationConsumer>();
        x.AddConsumer<SessionCompletedNotificationConsumer>();
        // Phase 4
        x.AddConsumer<RefundAppealApprovedNotificationConsumer>();
        x.AddConsumer<ConsultantSuspendedByAdminConsumer>();
        x.AddConsumer<ConsultantRestoredByAdminConsumer>();
        x.AddConsumer<UserDeactivatedByAdminConsumer>();
        x.AddConsumer<UserReactivatedByAdminConsumer>();
        // Phase 5
        x.AddConsumer<StudentOnboardingCompletedNotificationConsumer>();
        x.AddConsumer<ConsultantProfileActivatedNotificationConsumer>();
    });

    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Faaz Notification API", Version = "v1" });
    });

    var app = builder.Build();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        db.Database.Migrate();
        var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationTemplateSeeder");
        await Faaz.Services.Notification.WebHost.Seeding.NotificationTemplateSeeder.SeedAsync(scope.ServiceProvider, seederLogger);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "DB migration skipped — run migrations manually before using data endpoints");
    }

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Faaz Notification API v1"));
    }

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Notification service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
