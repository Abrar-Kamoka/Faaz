using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class BookingCancelledConsumer : IConsumer<BookingCancelledEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IRefundServices _refundServices;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<BookingCancelledConsumer> _logger;

        public BookingCancelledConsumer(IPaymentServices ps, IRefundServices rs, IPaymentGateway gw, ILogger<BookingCancelledConsumer> l)
        { _paymentServices = ps; _refundServices = rs; _gateway = gw; _logger = l; }

        public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
        {
            var msg     = context.Message;
            if (!msg.RefundRequired) return;

            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null) { _logger.LogWarning("BookingCancelledConsumer: no payment for booking {Id}", msg.BookingId); return; }
            if (payment.Status is not (PaymentStatus.Authorised or PaymentStatus.Captured)) return;

            if (payment.Status == PaymentStatus.Authorised)
            {
                var cancel = await _gateway.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
                if (cancel.Success)
                {
                    payment.Status = PaymentStatus.Cancelled;
                    await _paymentServices.SaveChangesAsync();
                }
                return;
            }

            // Captured — issue a partial or full refund
            if (payment.StripeChargeId is null) { _logger.LogWarning("No charge ID for payment {Id}", payment.Id); return; }

            var result = await _gateway.CreateRefundAsync(payment.StripeChargeId, msg.RefundAmount, msg.Reason ?? "booking_cancelled");
            if (!result.Success) { _logger.LogError("Refund failed for payment {Id}: {Err}", payment.Id, result.ErrorMessage); return; }

            var srNo   = await _refundServices.NewSerialNumberAsync();
            var refund = new Refund
            {
                SrNo           = srNo,
                PaymentId      = payment.Id,
                BookingId      = msg.BookingId,
                StudentUserId  = payment.StudentUserId,
                StripeRefundId = result.RefundId,
                Amount         = msg.RefundAmount,
                Status         = RefundStatus.Succeeded,
                Reason         = msg.Reason ?? "booking_cancelled"
            };
            await _refundServices.AddAsync(refund);
            payment.Status = msg.RefundAmount >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartialRefund;
            await _paymentServices.SaveChangesAsync();
        }
    }
}
