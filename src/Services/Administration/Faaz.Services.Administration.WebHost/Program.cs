using Faaz.BuildingBlocks.Extensions;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Extensions;
using Faaz.Services.Administration.WebHost.DevConsumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
    builder.Services.AddFaazOpenTelemetry(builder.Configuration, "faaz-administration");
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Faaz Administration API", Version = "v1" });
        opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "Enter your JWT access token"
        });
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                []
            }
        });
    });
    builder.Services.AddAdministrationInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddFaazRabbitMq(builder.Configuration, builder.Environment, x =>
    {
        x.AddConsumer<ConsultantSuspendedDevConsumer>();
        x.AddConsumer<ConsultantRestoredDevConsumer>();
        x.AddConsumer<UserDeactivatedDevConsumer>();
        x.AddConsumer<UserReactivatedDevConsumer>();
    });

    var app = builder.Build();

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        db.Database.Migrate();
        await Faaz.Services.Administration.Infrastructure.DatabaseContext.PlatformConfigSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "DB migration/seed skipped — run migrations manually before using data endpoints");
    }

    app.UseFaazMiddleware();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "Faaz Administration API v1"));
        app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Administration service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
