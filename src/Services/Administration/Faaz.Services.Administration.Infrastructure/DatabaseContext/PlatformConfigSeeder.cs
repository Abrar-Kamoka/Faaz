using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext;

public static class PlatformConfigSeeder
{
    private static readonly (string Key, string Value, string Description)[] Defaults =
    [
        ("CommissionRate",                "0.15", "Platform commission as fraction of session price (e.g. 0.15 = 15%)"),
        ("PayoutBufferHours",             "48",   "Hours after session completion before consultant payout releases"),
        ("ServiceFeeGbp",                 "2.50", "Fixed service fee charged to student per booking (GBP)"),
        ("MaxBookingsPerConsultantPerDay","8",     "Hard cap on daily accepted bookings per consultant"),
        ("SlotReservationMinutes",        "10",   "How long a Redis slot lock is held after CreateBooking"),
    ];

    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var logger      = scope.ServiceProvider.GetRequiredService<ILogger<AdminDbContext>>();
        var adminId     = Guid.Empty;

        foreach (var (key, value, description) in Defaults)
        {
            var exists = await db.PlatformConfigs.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Key == key);
            if (!exists)
            {
                var max = await db.PlatformConfigs.IgnoreQueryFilters()
                                  .MaxAsync(x => (int?)x.SrNo);
                await db.PlatformConfigs.AddAsync(new PlatformConfig
                {
                    SrNo                 = (max ?? 0) + 1,
                    Key                  = key,
                    Value                = value,
                    Description          = description,
                    LastUpdatedAt        = DateTime.UtcNow,
                    LastUpdatedByAdminId = adminId
                });
                logger.LogInformation("PlatformConfig seed: {Key} = {Value}", key, value);
            }
        }

        await db.SaveChangesAsync();
    }
}
