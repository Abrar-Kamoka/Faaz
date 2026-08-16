namespace Faaz.Services.Payment.Infrastructure.Services;

public interface IPaymentGateway
{
    Task<CreateIntentResult> CreatePaymentIntentAsync(
        decimal amountGbp, string? stripeCustomerId, string consultantConnectAccountId,
        decimal platformFeeGbp, string bookingIdMeta, CancellationToken ct = default);

    // Used to resume an abandoned-but-not-yet-captured checkout instead of blindly creating a
    // second PaymentIntent (and orphaning the first) every time the student comes back to pay.
    Task<RetrieveIntentResult> RetrievePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    Task<CaptureResult> CapturePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<CancelResult> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<RefundResult> CreateRefundAsync(string chargeId, decimal amountGbp, string reason, CancellationToken ct = default);
    Task<TransferResult> CreateTransferAsync(string connectAccountId, decimal amountGbp, string bookingId, CancellationToken ct = default);
}

public record CreateIntentResult(bool Success, string? PaymentIntentId, string? ClientSecret, string? ErrorMessage);
public record RetrieveIntentResult(bool Success, string? ClientSecret, string? Status, string? ErrorMessage);
public record CaptureResult(bool Success, string? ChargeId, string? ErrorMessage);
public record CancelResult(bool Success, string? ErrorMessage);
public record RefundResult(bool Success, string? RefundId, string? ErrorMessage);
public record TransferResult(bool Success, string? TransferId, string? ErrorMessage);
