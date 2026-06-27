using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class RefundAppealApprovedConsumer : IConsumer<RefundAppealApprovedEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IRefundServices _refundServices;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<RefundAppealApprovedConsumer> _logger;

        public RefundAppealApprovedConsumer(IPaymentServices ps, IRefundServices rs, IPaymentGateway gw, ILogger<RefundAppealApprovedConsumer> l)
        { _paymentServices = ps; _refundServices = rs; _gateway = gw; _logger = l; }

        public async Task Consume(ConsumeContext<RefundAppealApprovedEvent> context)
        {
            var msg     = context.Message;
            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null) { _logger.LogWarning("RefundAppealApprovedConsumer: no payment for booking {Id}", msg.BookingId); return; }
            if (payment.StripeChargeId is null) { _logger.LogWarning("No charge ID for payment {Id}", payment.Id); return; }

            var result = await _gateway.CreateRefundAsync(payment.StripeChargeId, msg.RefundAmountGbp, "goodwill_refund_appeal");
            if (!result.Success) { _logger.LogError("Appeal refund failed for payment {Id}: {Err}", payment.Id, result.ErrorMessage); return; }

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
                Reason         = "Appeal approved by admin",
                IsAppealRefund = true,
                AppealId       = msg.AppealId
            };
            await _refundServices.AddAsync(refund);
            payment.Status = payment.Amount <= msg.RefundAmountGbp ? PaymentStatus.Refunded : PaymentStatus.PartialRefund;
            await _paymentServices.SaveChangesAsync();
        }
    }
}
