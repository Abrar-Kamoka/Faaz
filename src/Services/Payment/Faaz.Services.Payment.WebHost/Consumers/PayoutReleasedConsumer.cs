using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class PayoutReleasedConsumer : IConsumer<PayoutReleasedEvent>
    {
        private readonly IPayoutServices _payoutServices;
        private readonly IPaymentConsultantClient _consultantClient;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<PayoutReleasedConsumer> _logger;

        public PayoutReleasedConsumer(
            IPayoutServices payoutServices,
            IPaymentConsultantClient consultantClient,
            IPaymentGateway gateway,
            ILogger<PayoutReleasedConsumer> logger)
        {
            _payoutServices    = payoutServices;
            _consultantClient  = consultantClient;
            _gateway           = gateway;
            _logger            = logger;
        }

        public async Task Consume(ConsumeContext<PayoutReleasedEvent> context)
        {
            var msg    = context.Message;
            var ct     = context.CancellationToken;
            var payout = await _payoutServices.GetByBookingIdAsync(msg.BookingId, ct);

            if (payout is null)
            {
                _logger.LogWarning("PayoutReleasedConsumer: no payout record for booking {Id}", msg.BookingId);
                return;
            }

            if (payout.Status == PayoutStatus.Paid)
            {
                _logger.LogDebug("PayoutReleasedConsumer: payout for booking {Id} already paid — skipping", msg.BookingId);
                return;
            }

            var connectAccountId = await _consultantClient.GetStripeConnectAccountIdAsync(msg.ConsultantId, ct);
            if (string.IsNullOrWhiteSpace(connectAccountId))
            {
                _logger.LogWarning("PayoutReleasedConsumer: no Stripe Connect account for consultant {Id}", msg.ConsultantId);
                payout.Status       = PayoutStatus.Failed;
                payout.FailureReason = "Consultant has no Stripe Connect account configured.";
                await _payoutServices.SaveChangesAsync(ct);
                return;
            }

            var result = await _gateway.CreateTransferAsync(connectAccountId, msg.NetAmount, msg.BookingId.ToString(), ct);

            if (result.Success)
            {
                payout.Status              = PayoutStatus.Paid;
                payout.StripeTransferId    = result.TransferId;
                payout.ReleasedAt          = DateTime.UtcNow;
                _logger.LogInformation("Payout released for booking {Id}, transfer {TransferId}", msg.BookingId, result.TransferId);
            }
            else
            {
                payout.Status        = PayoutStatus.Failed;
                payout.FailureReason = result.ErrorMessage;
                _logger.LogError("Payout failed for booking {Id}: {Error}", msg.BookingId, result.ErrorMessage);
            }

            await _payoutServices.SaveChangesAsync(ct);
        }
    }
}
