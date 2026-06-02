using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.DatabaseContext;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Infrastructure.Managers;

internal sealed class ConsultantApplicationManager : IConsultantApplicationServices
{
    private readonly ConsultantDbContext _db;

    public ConsultantApplicationManager(ConsultantDbContext db)
    {
        _db = db;
    }

    public Task<ConsultantApplication?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _db.ConsultantApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public Task<ConsultantApplication?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return _db.ConsultantApplications.FirstOrDefaultAsync(a => a.Email == email, ct);
    }

    public Task<ConsultantApplication?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct)
    {
        return _db.ConsultantApplications.FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber, ct);
    }

    public Task<ConsultantApplication?> GetByInviteTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        return _db.ConsultantApplications.FirstOrDefaultAsync(
            a => a.SetupInviteToken == tokenHash && a.SetupInviteTokenExpiry > DateTime.UtcNow, ct);
    }

    public async Task<(IReadOnlyList<ConsultantApplication> Items, int TotalCount)> GetPagedAsync(
        ConsultantApplicationStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantApplications.AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.ApplicationStatus == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<ConsultantApplication?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
    }

    public async Task AddAsync(ConsultantApplication application, CancellationToken ct)
    {
        await _db.ConsultantApplications.AddAsync(application, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
