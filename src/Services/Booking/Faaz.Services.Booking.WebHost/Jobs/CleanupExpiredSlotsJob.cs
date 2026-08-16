using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class CleanupExpiredSlotsJob : ICleanupExpiredSlotsJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISlotLockService _slotLock;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CleanupExpiredSlotsJob> _logger;

        public CleanupExpiredSlotsJob(IBookingServices b, ISlotLockService s, IPublishEndpoint p, ILogger<CleanupExpiredSlotsJob> l)
        { _bookingServices = b; _slotLock = s; _publishEndpoint = p; _logger = l; }

        public async Task ExecuteAsync()
        {
            // The slot lock itself (Redis/in-memory) expires on its own TTL — this job's job is the
            // Booking row, which otherwise sits in SlotReserved forever and permanently blocks the
            // slot (IsSlotTakenAsync treats SlotReserved as taken) if the student never completed payment.
            var expired = await _bookingServices.GetExpiredReservedSlotsAsync();

            foreach (var booking in expired)
            {
                var lockKey = $"slot:{booking.ConsultantProfileId}:{booking.ScheduledStartUtc:yyyyMMddHHmm}";
                await _slotLock.ReleaseAsync(lockKey);

                var fromStatus = booking.Status;
                booking.Status             = BookingStatus.CancelledTimeout;
                booking.CancellationReason = CancellationReason.Timeout;
                booking.CancellationNotes  = "Auto-expired: payment was not completed within the 10-minute slot hold";
                booking.RefundPercentage   = 100;

                await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
                {
                    BookingId  = booking.Id,
                    FromStatus = fromStatus,
                    ToStatus   = BookingStatus.CancelledTimeout,
                    ChangedAt  = DateTime.UtcNow,
                    Notes      = "Auto-expired: slot reservation window elapsed"
                });

                // Releases any dangling (Authorised but never Captured) PaymentIntent on the Payment side.
                await _publishEndpoint.Publish(new BookingCancelledEvent(
                    booking.Id, "system-timeout", "Slot reservation expired",
                    RefundRequired: true, RefundAmount: booking.TotalChargedGbp));
            }

            if (expired.Count > 0)
            {
                await _bookingServices.SaveChangesAsync();
                _logger.LogInformation("Expired {Count} unpaid slot reservations", expired.Count);
            }
        }
    }
}
