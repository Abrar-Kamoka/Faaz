using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Faaz.Services.Payment.WebHost.Features.Payments.Commands
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;
    using DomainRefund = global::Faaz.Services.Payment.Domain.Entities.Refund;
    using StripeEvent = global::Stripe.Event;
    using static global::Faaz.Services.Payment.Domain.PaymentEnums;

    public class ProcessStripeWebhookCommand : IRequest
    {
        public string Payload    { get; set; } = "";
        public string SignatureHeader { get; set; } = "";
    }

    public class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IRefundServices _refundServices;
        private readonly IStripeWebhookEventServices _webhookServices;
        private readonly IPaymentGateway _gateway;
        private readonly IPaymentConsultantClient _consultantClient;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;
        private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;

        public ProcessStripeWebhookCommandHandler(
            IPaymentServices ps, IRefundServices rs, IStripeWebhookEventServices ws,
            IPaymentGateway gw, IPaymentConsultantClient cc, IPublishEndpoint pub, IConfiguration config, ILogger<ProcessStripeWebhookCommandHandler> l)
        { _paymentServices = ps; _refundServices = rs; _webhookServices = ws; _gateway = gw; _consultantClient = cc; _publishEndpoint = pub; _config = config; _logger = l; }

        public async Task Handle(ProcessStripeWebhookCommand command, CancellationToken ct)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe:WebhookSecret not configured.");

            StripeEvent stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(command.Payload, command.SignatureHeader, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature validation failed");
                throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error("Invalid webhook signature.", "webhook.invalid-signature");
            }

            // Idempotency check
            if (await _webhookServices.IsProcessedAsync(stripeEvent.Id, ct))
            {
                _logger.LogDebug("Webhook {Id} already processed — skipping", stripeEvent.Id);
                return;
            }

            var record = new StripeWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType     = stripeEvent.Type,
                PayloadJson   = command.Payload,
                ReceivedAt    = DateTime.UtcNow
            };

            try
            {
                await HandleStripeEventAsync(stripeEvent, ct);
                record.Processed   = true;
                record.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                record.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to process Stripe event {Id} of type {Type}", stripeEvent.Id, stripeEvent.Type);
                throw;
            }
            finally
            {
                await _webhookServices.AddAsync(record, ct);
                await _webhookServices.SaveChangesAsync(ct);
            }
        }

        private async Task HandleStripeEventAsync(StripeEvent stripeEvent, CancellationToken ct)
        {
            switch (stripeEvent.Type)
            {
                case "payment_intent.amount_capturable_updated":
                    await HandlePaymentIntentAuthorizedAsync((PaymentIntent)stripeEvent.Data.Object, ct);
                    break;
                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync((PaymentIntent)stripeEvent.Data.Object, ct);
                    break;
                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailedAsync((PaymentIntent)stripeEvent.Data.Object, ct);
                    break;
                case "charge.refunded":
                    await HandleChargeRefundedAsync((Charge)stripeEvent.Data.Object, ct);
                    break;
                case "account.updated":
                    await HandleAccountUpdatedAsync((Account)stripeEvent.Data.Object, ct);
                    break;
                default:
                    _logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }
        }

        // Manual-capture intent reached requires_capture — the student's card was successfully
        // authorized (funds held). This is the earliest reliable signal that checkout finished;
        // payment_intent.succeeded doesn't fire until the platform later captures on acceptance.
        private async Task HandlePaymentIntentAuthorizedAsync(PaymentIntent intent, CancellationToken ct)
        {
            var payment = await _paymentServices.GetByStripePaymentIntentIdAsync(intent.Id, ct);
            if (payment is null) { _logger.LogWarning("No payment found for intent {Id}", intent.Id); return; }

            await _publishEndpoint.Publish(new PaymentAuthorizedEvent(payment.BookingId, intent.Id, payment.StudentUserId, payment.Amount), ct);
        }

        private async Task HandlePaymentIntentSucceededAsync(PaymentIntent intent, CancellationToken ct)
        {
            var payment = await _paymentServices.GetByStripePaymentIntentIdAsync(intent.Id, ct);
            if (payment is null) { _logger.LogWarning("No payment found for intent {Id}", intent.Id); return; }

            payment.Status         = PaymentStatus.Captured;
            payment.StripeChargeId = intent.LatestChargeId;
            await _paymentServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new PaymentCapturedEvent(payment.BookingId, intent.Id, payment.Amount), ct);
        }

        private async Task HandlePaymentIntentFailedAsync(PaymentIntent intent, CancellationToken ct)
        {
            var payment = await _paymentServices.GetByStripePaymentIntentIdAsync(intent.Id, ct);
            if (payment is null) return;

            payment.Status         = PaymentStatus.Failed;
            payment.FailureMessage = intent.LastPaymentError?.Message;
            await _paymentServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new PaymentFailedEvent(payment.BookingId, intent.Id, payment.FailureMessage ?? ""), ct);
        }

        private async Task HandleChargeRefundedAsync(Charge charge, CancellationToken ct)
        {
            var payment = await _paymentServices.GetByStripePaymentIntentIdAsync(charge.PaymentIntentId, ct);
            if (payment is null) return;

            foreach (var stripeRefund in charge.Refunds)
            {
                var existing = await _refundServices.GetByBookingIdAsync(payment.BookingId, ct);
                if (existing.Any(r => r.StripeRefundId == stripeRefund.Id)) continue;

                var srNo   = await _refundServices.NewSerialNumberAsync(ct);
                var refund = new DomainRefund
                {
                    SrNo          = srNo,
                    PaymentId     = payment.Id,
                    BookingId     = payment.BookingId,
                    StudentUserId = payment.StudentUserId,
                    StripeRefundId = stripeRefund.Id,
                    Amount        = stripeRefund.Amount / 100m,
                    Status        = RefundStatus.Succeeded,
                    Reason        = stripeRefund.Reason ?? "stripe_refund"
                };
                await _refundServices.AddAsync(refund, ct);
            }

            payment.Status = PaymentStatus.Refunded;
            await _paymentServices.SaveChangesAsync(ct);
        }

        // Authoritative signal that a consultant's Connect onboarding/capabilities changed — syncs
        // the flags Consultant service uses to gate profile completeness and public bookability.
        private async Task HandleAccountUpdatedAsync(Account account, CancellationToken ct)
        {
            await _consultantClient.UpdateStripeConnectAccountStatusAsync(
                account.Id, account.DetailsSubmitted, account.ChargesEnabled, ct);
        }
    }
}
