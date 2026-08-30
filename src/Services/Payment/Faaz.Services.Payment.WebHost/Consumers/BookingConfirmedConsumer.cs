using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    // Captures the funds authorized at checkout — with CaptureMethod=manual (see
    // StripePaymentGateway.CreatePaymentIntentAsync), nothing else in the system ever charges the
    // card. Without this consumer, an accepted, fully-authorized booking never actually gets paid.
    // Only triggers the Stripe-side capture; the resulting payment_intent.succeeded webhook (see
    // ProcessStripeWebhookCommand) remains the single place that marks the Payment row Captured.
    public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IPaymentGateway _gateway;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<BookingConfirmedConsumer> _logger;

        public BookingConfirmedConsumer(IPaymentServices ps, IPaymentGateway gw, IPublishEndpoint pub, ILogger<BookingConfirmedConsumer> l)
        { _paymentServices = ps; _gateway = gw; _publishEndpoint = pub; _logger = l; }

        public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
        {
            var msg = context.Message;
            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null)
            {
                _logger.LogWarning("BookingConfirmedConsumer: no payment found for booking {Id}", msg.BookingId);
                return;
            }

            // Idempotent — a redelivered event must not attempt a second capture on an intent
            // that's already been captured (or otherwise moved out of Authorised).
            if (payment.Status != PaymentStatus.Authorised) return;

            var result = await _gateway.CapturePaymentIntentAsync(payment.StripePaymentIntentId);
            if (!result.Success)
            {
                // The consultant already accepted (AcceptBookingCommand set the booking Confirmed and
                // scheduled the session/reminders BEFORE this async capture attempt runs), but the card
                // that was authorized at checkout can still fail here — expired/cancelled between
                // authorization and acceptance, issuer fraud block, etc. Without this, the booking would
                // stay stuck "Confirmed" with no money ever actually collected and nothing telling anyone.
                // Reuse the same PaymentFailedEvent/consumer the initial-authorization-failure path uses
                // so Booking's existing rollback (CancelledPaymentFailed) handles this uniformly.
                _logger.LogError(
                    "Capture failed for booking {BookingId}, intent {IntentId}: {Error}",
                    msg.BookingId, payment.StripePaymentIntentId, result.ErrorMessage);

                payment.Status         = PaymentStatus.Failed;
                payment.FailureMessage = result.ErrorMessage;

                // Published before SaveChangesAsync so the EF outbox captures it atomically.
                await _publishEndpoint.Publish(new PaymentFailedEvent(
                    payment.BookingId, payment.StripePaymentIntentId, result.ErrorMessage ?? "Capture failed"));

                await _paymentServices.SaveChangesAsync();
            }
        }
    }
}
