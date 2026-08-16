using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class CreateBookingCommand : IRequest<Guid>
    {
        public Guid RequestingStudentId { get; set; }
        public CreateBookingDto PostModel { get; set; } = null!;
    }

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IBookingConsultantClient _consultantClient;
        private readonly ISlotLockService _slotLock;

        public CreateBookingCommandHandler(IBookingServices b, IBookingConsultantClient c, ISlotLockService s)
        { _bookingServices = b; _consultantClient = c; _slotLock = s; }

        public async Task<Guid> Handle(CreateBookingCommand command, CancellationToken ct)
        {
            var dto = command.PostModel;

            var slotCheck = await _consultantClient.CheckSlotAvailabilityAsync(dto.ConsultantProfileId, dto.SessionTypeId, dto.ScheduledStartUtc, ct);
            if (slotCheck is null || !slotCheck.IsAvailable)
                throw BusinessRuleException.Error("The selected time slot is not available.", "slot.unavailable");

            var lockKey = $"slot:{dto.ConsultantProfileId}:{dto.ScheduledStartUtc:yyyyMMddHHmm}";
            var acquired = await _slotLock.TryAcquireAsync(lockKey, TimeSpan.FromMinutes(10), ct);
            if (!acquired)
                throw BusinessRuleException.Error("The selected time slot was just taken. Please choose another.", "slot.taken");

            if (await _bookingServices.IsSlotTakenAsync(dto.ConsultantProfileId, dto.ScheduledStartUtc, ct))
            {
                await _slotLock.ReleaseAsync(lockKey, ct);
                throw BusinessRuleException.Error("The selected time slot is no longer available.", "slot.taken");
            }

            var srNo    = await _bookingServices.NewSerialNumberAsync(ct);
            var booking = new Booking
            {
                SrNo                  = srNo,
                StudentUserId         = command.RequestingStudentId,
                ConsultantUserId      = slotCheck.ConsultantUserId,
                ConsultantProfileId   = dto.ConsultantProfileId,
                SessionTypeId         = dto.SessionTypeId,
                SessionTypeName       = slotCheck.SessionTypeName,
                SessionPriceGbp       = slotCheck.SessionPriceGbp,
                PlatformCommissionGbp = Math.Round(slotCheck.SessionPriceGbp * 0.15m, 2),
                TotalChargedGbp       = slotCheck.SessionPriceGbp,
                DurationMinutes       = slotCheck.DurationMinutes,
                CallType              = (CallType)dto.CallType,
                ScheduledStartUtc     = dto.ScheduledStartUtc,
                ScheduledEndUtc       = dto.ScheduledStartUtc.AddMinutes(slotCheck.DurationMinutes),
                StudentTimezone       = dto.StudentTimezone,
                SessionBrief          = dto.SessionBrief,
                PromoCodeId           = dto.PromoCodeId,
                Status                = BookingStatus.SlotReserved,
                SlotReservedUntilUtc  = DateTime.UtcNow.AddMinutes(10)
            };

            await _bookingServices.AddAsync(booking, ct);
            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id,
                FromStatus      = BookingStatus.SlotReserved,
                ToStatus        = BookingStatus.SlotReserved,
                ChangedByUserId = command.RequestingStudentId,
                Notes           = "Booking created — slot reserved"
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);

            // The consultant is NOT notified here — a SlotReserved booking isn't a real request
            // yet (payment hasn't been authorized). See PaymentAuthorizedConsumer, which publishes
            // BookingRequestReceivedEvent once the booking actually becomes PendingConfirmation.

            return booking.Id;
        }
    }
}
