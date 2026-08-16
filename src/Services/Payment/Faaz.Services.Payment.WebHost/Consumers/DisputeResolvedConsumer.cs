using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class DisputeResolvedConsumer : IConsumer<DisputeResolvedEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IRefundServices _refundServices;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<DisputeResolvedConsumer> _logger;

        public DisputeResolvedConsumer(IPaymentServices ps, IRefundServices rs, IPaymentGateway gw, ILogger<DisputeResolvedConsumer> l)
        { _paymentServices = ps; _refundServices = rs; _gateway = gw; _logger = l; }

        public async Task Consume(ConsumeContext<DisputeResolvedEvent> context)
        {
            var msg = context.Message;

            // "favour_consultant" / "no_action" close the dispute with no money movement.
            if (msg.Resolution != "favour_student" || msg.RefundAmountGbp <= 0m)
                return;

            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null) { _logger.LogWarning("DisputeResolvedConsumer: no payment for booking {Id}", msg.BookingId); return; }
            if (payment.StripeChargeId is null) { _logger.LogWarning("No charge ID for payment {Id}", payment.Id); return; }

            var result = await _gateway.CreateRefundAsync(payment.StripeChargeId, msg.RefundAmountGbp, "dispute_resolved_favour_student");
            if (!result.Success) { _logger.LogError("Dispute refund failed for payment {Id}: {Err}", payment.Id, result.ErrorMessage); return; }

            var srNo   = await _refundServices.NewSerialNumberAsync();
            var refund = new Refund
            {
                SrNo           = srNo,
                PaymentId      = payment.Id,
                BookingId      = msg.BookingId,
                StudentUserId  = msg.StudentUserId,
                StripeRefundId = result.RefundId,
                Amount         = msg.RefundAmountGbp,
                Status         = RefundStatus.Succeeded,
                Reason         = "Dispute resolved in favour of student",
                IsAppealRefund = false
            };
            await _refundServices.AddAsync(refund);
            payment.Status = payment.Amount <= msg.RefundAmountGbp ? PaymentStatus.Refunded : PaymentStatus.PartialRefund;
            await _paymentServices.SaveChangesAsync();
            _logger.LogInformation("DisputeResolvedConsumer: refunded £{Amount} for booking {Id}", msg.RefundAmountGbp, msg.BookingId);
        }
    }
}
