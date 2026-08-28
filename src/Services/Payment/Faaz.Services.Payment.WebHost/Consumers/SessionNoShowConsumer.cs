using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    // Fault-based no-show policy:
    //  - Consultant no-show (student showed up, consultant didn't): full refund, no payout.
    //  - Student no-show (consultant showed up, student didn't): no refund — the consultant held
    //    the slot and gets paid as if the session happened.
    //  - Both no-show: full refund, no payout — no service was rendered by either side.
    public class SessionNoShowConsumer : IConsumer<SessionNoShowEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IRefundServices _refundServices;
        private readonly IPayoutServices _payoutServices;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<SessionNoShowConsumer> _logger;

        public SessionNoShowConsumer(
            IPaymentServices ps, IRefundServices rs, IPayoutServices pos, IPaymentGateway gw,
            ILogger<SessionNoShowConsumer> l)
        { _paymentServices = ps; _refundServices = rs; _payoutServices = pos; _gateway = gw; _logger = l; }

        public async Task Consume(ConsumeContext<SessionNoShowEvent> context)
        {
            var msg = context.Message;

            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null) { _logger.LogWarning("SessionNoShowConsumer: no payment for booking {Id}", msg.BookingId); return; }

            var studentAtFaultOnly = !msg.StudentJoined && msg.ConsultantJoined;
            if (studentAtFaultOnly)
            {
                var existingPayout = await _payoutServices.GetByBookingIdAsync(msg.BookingId);
                if (existingPayout is not null) return;

                var payoutSrNo = await _payoutServices.NewSerialNumberAsync();
                await _payoutServices.AddAsync(new Payout
                {
                    SrNo               = payoutSrNo,
                    BookingId          = msg.BookingId,
                    ConsultantUserId   = payment.ConsultantUserId,
                    Amount             = payment.ConsultantPayout,
                    Status             = PayoutStatus.Pending,
                    ScheduledReleaseAt = DateTime.UtcNow.AddHours(48)
                });
                await _payoutServices.SaveChangesAsync();

                _logger.LogInformation("No-show payout created for booking {Id} (student no-show, consultant held the slot)", msg.BookingId);
                return;
            }

            // Every other case (consultant no-show, or both no-show) — the student didn't get a
            // session either way, so they're made whole.
            if (payment.Status is not (PaymentStatus.Authorised or PaymentStatus.Captured)) return;

            if (payment.Status == PaymentStatus.Authorised)
            {
                var cancel = await _gateway.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
                if (cancel.Success) { payment.Status = PaymentStatus.Cancelled; await _paymentServices.SaveChangesAsync(); }
                return;
            }

            if (payment.StripeChargeId is null) { _logger.LogWarning("No charge ID for payment {Id}", payment.Id); return; }

            var reason = !msg.StudentJoined && !msg.ConsultantJoined ? "both_no_show" : "consultant_no_show";
            var result = await _gateway.CreateRefundAsync(payment.StripeChargeId, payment.Amount, reason);
            if (!result.Success) { _logger.LogError("No-show refund failed for payment {Id}: {Err}", payment.Id, result.ErrorMessage); return; }

            var refundSrNo = await _refundServices.NewSerialNumberAsync();
            await _refundServices.AddAsync(new Refund
            {
                SrNo           = refundSrNo,
                PaymentId      = payment.Id,
                BookingId      = msg.BookingId,
                StudentUserId  = payment.StudentUserId,
                StripeRefundId = result.RefundId,
                Amount         = payment.Amount,
                Status         = RefundStatus.Succeeded,
                Reason         = reason
            });
            payment.Status = PaymentStatus.Refunded;
            await _paymentServices.SaveChangesAsync();
            await _refundServices.SaveChangesAsync();

            _logger.LogInformation("No-show refund issued for booking {Id}: {Reason}", msg.BookingId, reason);
        }
    }
}
