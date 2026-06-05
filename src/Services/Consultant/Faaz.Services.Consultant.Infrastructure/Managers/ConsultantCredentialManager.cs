using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.DatabaseContext;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Consultant.Infrastructure.Managers;

internal sealed class ConsultantCredentialManager : IConsultantCredentialServices
{
    private readonly ConsultantDbContext _db;

    public ConsultantCredentialManager(ConsultantDbContext db)
    {
        _db = db;
    }

    public async Task<ConsultantCredential?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.ConsultantCredentials.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<ConsultantCredential>> GetByProfileIdAsync(Guid profileId, CancellationToken ct)
    {
        return await _db.ConsultantCredentials
            .Where(c => c.ConsultantProfileId == profileId)
            .OrderByDescending(c => c.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ConsultantCredential credential, CancellationToken ct)
    {
        await _db.ConsultantCredentials.AddAsync(credential, ct);
    }

    public void Delete(ConsultantCredential credential)
    {
        credential.IsDeleted = true;
        credential.DeletedAt = DateTime.UtcNow;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }
}
