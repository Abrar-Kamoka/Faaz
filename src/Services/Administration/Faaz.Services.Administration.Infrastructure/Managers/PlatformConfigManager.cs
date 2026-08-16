using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class PlatformConfigManager : IPlatformConfigServices
{
    private readonly AdminDbContext _db;
    public PlatformConfigManager(AdminDbContext db) { _db = db; }

    public async Task<PlatformConfig?> GetByKeyAsync(string key, CancellationToken ct = default)
        => await _db.PlatformConfigs.FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task<IReadOnlyList<PlatformConfig>> GetAllAsync(CancellationToken ct = default)
        => await _db.PlatformConfigs.OrderBy(x => x.Key).ToListAsync(ct);

    public async Task UpsertAsync(string key, string value, string? description, Guid adminId, CancellationToken ct = default)
    {
        var existing = await _db.PlatformConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Key == key, ct);

        if (existing is null)
        {
            var srNo = await NewSerialNumberAsync(ct);
            await _db.PlatformConfigs.AddAsync(new PlatformConfig
            {
                SrNo                 = srNo,
                Key                  = key,
                Value                = value,
                Description          = description,
                LastUpdatedAt        = DateTime.UtcNow,
                LastUpdatedByAdminId = adminId
            }, ct);
        }
        else
        {
            existing.Value                = value;
            existing.Description          = description ?? existing.Description;
            existing.LastUpdatedAt        = DateTime.UtcNow;
            existing.LastUpdatedByAdminId = adminId;
            existing.IsDeleted            = false;
        }
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.PlatformConfigs.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
