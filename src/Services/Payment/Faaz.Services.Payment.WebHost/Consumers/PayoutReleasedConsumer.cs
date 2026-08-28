using Faaz.Services.Payment.Domain.Entities;
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
        private readonly IPaymentServices _paymentServices;
        private readonly IPaymentConsultantClient _consultantClient;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<PayoutReleasedConsumer> _logger;

        public PayoutReleasedConsumer(
            IPayoutServices payoutServices,
            IPaymentServices paymentServices,
            IPaymentConsultantClient consultantClient,
            IPaymentGateway gateway,
            ILogger<PayoutReleasedConsumer> logger)
        {
            _payoutServices    = payoutServices;
            _paymentServices   = paymentServices;
            _consultantClient  = consultantClient;
            _gateway           = gateway;
            _logger            = logger;
        }

        public async Task Consume(ConsumeContext<PayoutReleasedEvent> context)
        {
            var msg    = context.Message;
            var ct     = context.CancellationToken;
            var payout = await _payoutServices.GetByBookingIdAsync(msg.BookingId, ct);

            // Self-heal a missing Payout row instead of silently no-op'ing. Normally SessionCompletedConsumer
            // creates this row the moment the session ends; if that event was ever lost/delayed, the booking
            // would otherwise sit forever marked Settled (Booking's ReleasePendingPayoutsJob already moved it
            // there) while the consultant was never actually paid, with nothing surfacing the gap.
            if (payout is null)
            {
                var paymentForBooking = await _paymentServices.GetByBookingIdAsync(msg.BookingId, ct);
                if (paymentForBooking is null)
                {
                    _logger.LogWarning("PayoutReleasedConsumer: no payout AND no payment record for booking {Id} — cannot reconstruct payout amount", msg.BookingId);
                    return;
                }

                var srNo = await _payoutServices.NewSerialNumberAsync(ct);
                payout = new Payout
                {
                    SrNo             = srNo,
                    BookingId        = msg.BookingId,
                    ConsultantUserId = paymentForBooking.ConsultantUserId,
                    Amount           = paymentForBooking.ConsultantPayout,
                    Status           = PayoutStatus.Pending
                };
                await _payoutServices.AddAsync(payout, ct);
                _logger.LogWarning("PayoutReleasedConsumer: reconstructed missing payout row for booking {Id} from payment record", msg.BookingId);
            }

            if (payout.Status == PayoutStatus.Paid)
            {
                _logger.LogDebug("PayoutReleasedConsumer: payout for booking {Id} already paid — skipping", msg.BookingId);
                return;
            }

            // A dispute (chargeback) landed on this booking's charge before the escrow window closed —
            // see ProcessStripeWebhookCommand's charge.dispute.created handler. Don't pay the consultant
            // out while that's unresolved; an admin has to clear it manually once the dispute closes.
            if (payout.Status == PayoutStatus.OnHold)
            {
                _logger.LogWarning("PayoutReleasedConsumer: payout for booking {Id} is on hold (dispute) — skipping release", msg.BookingId);
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

            // Use the Payment-side amount (payout.Amount), not msg.NetAmount — msg.NetAmount is computed
            // independently by Booking from its own (pre-promo-discount) TotalChargedGbp/PlatformCommissionGbp
            // fields, which can diverge from what was actually captured on Stripe once a promo code is
            // involved. payout.Amount traces back to Payment.ConsultantPayout, computed at charge time from
            // the actual discounted amount and the actual commission rate applied — the authoritative figure.
            var result = await _gateway.CreateTransferAsync(connectAccountId, payout.Amount, msg.BookingId.ToString(), ct);

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
