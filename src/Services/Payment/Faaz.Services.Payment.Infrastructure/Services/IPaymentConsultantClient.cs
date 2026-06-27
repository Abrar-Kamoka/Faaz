namespace Faaz.Services.Payment.Infrastructure.Services;

public interface IPaymentConsultantClient
{
    Task<string?> GetStripeConnectAccountIdAsync(Guid consultantUserId, CancellationToken ct = default);
}
