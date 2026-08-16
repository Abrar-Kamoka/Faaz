using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace Faaz.Services.Identity.IntegrationTests.Infrastructure;

public class IdentityWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<IdentityDbContext>(opts =>
                opts.UseSqlServer(_db.GetConnectionString()));

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Test");
    }

    public new async Task DisposeAsync()
    {
        await _db.StopAsync();
        await base.DisposeAsync();
    }
}
