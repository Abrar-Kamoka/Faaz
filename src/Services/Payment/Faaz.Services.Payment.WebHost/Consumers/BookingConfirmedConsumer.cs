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
        private readonly ILogger<BookingConfirmedConsumer> _logger;

        public BookingConfirmedConsumer(IPaymentServices ps, IPaymentGateway gw, ILogger<BookingConfirmedConsumer> l)
        { _paymentServices = ps; _gateway = gw; _logger = l; }

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
                _logger.LogError(
                    "Capture failed for booking {BookingId}, intent {IntentId}: {Error}",
                    msg.BookingId, payment.StripePaymentIntentId, result.ErrorMessage);
        }
    }
}
