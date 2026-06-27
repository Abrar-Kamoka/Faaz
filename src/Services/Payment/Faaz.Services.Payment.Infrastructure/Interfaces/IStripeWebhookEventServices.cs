using Faaz.Services.Payment.Domain.Entities;

namespace Faaz.Services.Payment.Infrastructure.Interfaces;

public interface IStripeWebhookEventServices
{
    Task<StripeWebhookEvent?> GetByStripeEventIdAsync(string stripeEventId, CancellationToken ct = default);
    Task<bool> IsProcessedAsync(string stripeEventId, CancellationToken ct = default);
    Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
