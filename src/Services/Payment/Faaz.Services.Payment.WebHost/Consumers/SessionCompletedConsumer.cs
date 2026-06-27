using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Payment.WebHost.Consumers
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class SessionCompletedConsumer : IConsumer<SessionCompletedEvent>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IPayoutServices _payoutServices;
        private readonly ILogger<SessionCompletedConsumer> _logger;

        public SessionCompletedConsumer(IPaymentServices ps, IPayoutServices po, ILogger<SessionCompletedConsumer> l)
        { _paymentServices = ps; _payoutServices = po; _logger = l; }

        public async Task Consume(ConsumeContext<SessionCompletedEvent> context)
        {
            var msg     = context.Message;
            var payment = await _paymentServices.GetByBookingIdAsync(msg.BookingId);
            if (payment is null) { _logger.LogWarning("SessionCompletedConsumer: no payment for booking {Id}", msg.BookingId); return; }

            // Create a pending payout scheduled for 48h after completion
            var existing = await _payoutServices.GetByBookingIdAsync(msg.BookingId);
            if (existing is not null) return; // already created

            var srNo   = await _payoutServices.NewSerialNumberAsync();
            var payout = new Payout
            {
                SrNo                   = srNo,
                BookingId              = msg.BookingId,
                ConsultantUserId       = payment.ConsultantUserId,
                Amount                 = payment.ConsultantPayout,
                Status                 = PayoutStatus.Pending,
                ScheduledReleaseAt     = msg.CompletedAt.UtcDateTime.AddHours(48)
            };
            await _payoutServices.AddAsync(payout);
            await _payoutServices.SaveChangesAsync();
        }
    }
}
