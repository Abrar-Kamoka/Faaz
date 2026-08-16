namespace Faaz.Services.Payment.Infrastructure.Services;

public readonly record struct StripeConnectStatus(string? AccountId, bool DetailsSubmitted, bool ChargesEnabled);

public interface IPaymentConsultantClient
{
    Task<string?> GetStripeConnectAccountIdAsync(Guid consultantUserId, CancellationToken ct = default);

    // Reads the LOCAL, webhook-synced status (no live Stripe call) — used to gate payment creation
    // without adding a Stripe round-trip to the booking-payment critical path.
    Task<StripeConnectStatus> GetStripeConnectStatusAsync(Guid consultantUserId, CancellationToken ct = default);
    Task SetStripeConnectAccountIdAsync(Guid consultantUserId, string stripeAccountId, CancellationToken ct = default);

    // Called from the Stripe "account.updated" webhook — the account ID is the only correlation
    // key a Connect webhook payload carries, so lookup happens by account ID, not userId.
    Task UpdateStripeConnectAccountStatusAsync(string stripeAccountId, bool detailsSubmitted, bool chargesEnabled, CancellationToken ct = default);
}
