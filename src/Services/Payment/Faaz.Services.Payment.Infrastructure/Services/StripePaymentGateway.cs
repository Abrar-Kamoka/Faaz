using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Faaz.Services.Payment.Infrastructure.Services;

internal sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(IConfiguration config, ILogger<StripePaymentGateway> logger)
    {
        _logger = logger;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey not configured.");
    }

    public async Task<CreateIntentResult> CreatePaymentIntentAsync(
        decimal amountGbp, string? stripeCustomerId, string consultantConnectAccountId,
        decimal platformFeeGbp, string bookingIdMeta, CancellationToken ct = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount        = (long)(amountGbp * 100),
                Currency      = "gbp",
                Customer      = stripeCustomerId,
                CaptureMethod = "manual",
                Metadata      = new Dictionary<string, string> { ["booking_id"] = bookingIdMeta },
                TransferData  = new PaymentIntentTransferDataOptions
                {
                    Destination = consultantConnectAccountId
                },
                ApplicationFeeAmount = (long)(platformFeeGbp * 100)
            };
            var service = new PaymentIntentService();
            var intent  = await service.CreateAsync(options, cancellationToken: ct);
            return new CreateIntentResult(true, intent.Id, intent.ClientSecret, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe CreatePaymentIntent failed for booking {BookingId}", bookingIdMeta);
            return new CreateIntentResult(false, null, null, ex.Message);
        }
    }

    public async Task<RetrieveIntentResult> RetrievePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var intent  = await service.GetAsync(paymentIntentId, cancellationToken: ct);
            return new RetrieveIntentResult(true, intent.ClientSecret, intent.Status, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe retrieve failed for intent {IntentId}", paymentIntentId);
            return new RetrieveIntentResult(false, null, null, ex.Message);
        }
    }

    public async Task<CaptureResult> CapturePaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var intent  = await service.CaptureAsync(paymentIntentId, cancellationToken: ct);
            var chargeId = intent.LatestChargeId;
            return new CaptureResult(true, chargeId, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe capture failed for intent {IntentId}", paymentIntentId);
            return new CaptureResult(false, null, ex.Message);
        }
    }

    public async Task<CancelResult> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            await service.CancelAsync(paymentIntentId, cancellationToken: ct);
            return new CancelResult(true, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe cancel failed for intent {IntentId}", paymentIntentId);
            return new CancelResult(false, ex.Message);
        }
    }

    public async Task<RefundResult> CreateRefundAsync(string chargeId, decimal amountGbp, string reason, CancellationToken ct = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                Charge = chargeId,
                Amount = (long)(amountGbp * 100),
                Reason = "requested_by_customer"
            };
            var service = new RefundService();
            var refund  = await service.CreateAsync(options, cancellationToken: ct);
            return new RefundResult(true, refund.Id, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for charge {ChargeId}", chargeId);
            return new RefundResult(false, null, ex.Message);
        }
    }

    public async Task<TransferResult> CreateTransferAsync(string connectAccountId, decimal amountGbp, string bookingId, CancellationToken ct = default)
    {
        try
        {
            var options = new TransferCreateOptions
            {
                Amount      = (long)(amountGbp * 100),
                Currency    = "gbp",
                Destination = connectAccountId,
                Metadata    = new Dictionary<string, string> { ["booking_id"] = bookingId }
            };
            var service  = new TransferService();
            var transfer = await service.CreateAsync(options, cancellationToken: ct);
            return new TransferResult(true, transfer.Id, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe transfer failed for booking {BookingId} to account {Account}", bookingId, connectAccountId);
            return new TransferResult(false, null, ex.Message);
        }
    }
}
