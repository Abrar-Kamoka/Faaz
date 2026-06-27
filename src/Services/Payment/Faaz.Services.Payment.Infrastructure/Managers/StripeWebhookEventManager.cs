using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.Managers;

internal sealed class StripeWebhookEventManager : IStripeWebhookEventServices
{
    private readonly PaymentDbContext _db;

    public StripeWebhookEventManager(PaymentDbContext db) { _db = db; }

    public async Task<StripeWebhookEvent?> GetByStripeEventIdAsync(string stripeEventId, CancellationToken ct = default)
        => await _db.StripeWebhookEvents.FirstOrDefaultAsync(x => x.StripeEventId == stripeEventId, ct);

    public async Task<bool> IsProcessedAsync(string stripeEventId, CancellationToken ct = default)
        => await _db.StripeWebhookEvents.AnyAsync(x => x.StripeEventId == stripeEventId && x.Processed, ct);

    public async Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken ct = default)
        => await _db.StripeWebhookEvents.AddAsync(webhookEvent, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
