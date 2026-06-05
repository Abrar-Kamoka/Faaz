using System.Security.Claims;
using Faaz.SharedKernel.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Faaz.BuildingBlocks.Persistence;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;

    public AuditableEntityInterceptor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now    = DateTime.UtcNow;
        var userId = _http.HttpContext?.User.FindFirstValue("sub") is string s
            && Guid.TryParse(s, out var id) ? id : (Guid?)null;

        foreach (var entry in context.ChangeTracker.Entries<Trackable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (userId.HasValue) entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (userId.HasValue) entry.Entity.UpdatedBy = userId;
            }
        }

        // Stamp soft deletes — fires when IsDeleted flips from false → true.
        foreach (var entry in context.ChangeTracker.Entries<BaseSoftDeleteModel>())
        {
            if (entry.State == EntityState.Modified
                && entry.OriginalValues.GetValue<bool>(nameof(BaseEntity.IsDeleted)) == false
                && entry.Entity.IsDeleted)
            {
                entry.Entity.DeletedAt = now;
                if (userId.HasValue) entry.Entity.DeletedBy = userId;
            }
        }

        // SrNo is set on INSERT only — never update it once persisted.
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified) continue;
            var srNo = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "SrNo");
            if (srNo is not null) srNo.IsModified = false;
        }
    }
}
