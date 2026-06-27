using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using StackExchange.Redis;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Queries;

public class GetAvailableSlotsQuery : IRequest<IReadOnlyList<DateTime>>
{
    public Guid     ConsultantProfileId { get; set; }
    public Guid     SessionTypeId       { get; set; }
    public DateOnly From                { get; set; }
    public DateOnly To                  { get; set; }
}

internal sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<DateTime>>
{
    private readonly IBookingConsultantClient _consultantClient;
    private readonly IBookingServices         _bookingServices;
    private readonly IConnectionMultiplexer   _redis;

    public GetAvailableSlotsQueryHandler(
        IBookingConsultantClient consultantClient,
        IBookingServices bookingServices,
        IConnectionMultiplexer redis)
    {
        _consultantClient = consultantClient;
        _bookingServices  = bookingServices;
        _redis            = redis;
    }

    public async Task<IReadOnlyList<DateTime>> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        if ((query.To.DayNumber - query.From.DayNumber) > 60)
            throw BusinessRuleException.Error("Date range must not exceed 60 days.", "slots.range-too-wide");

        var schedule = await _consultantClient.GetConsultantScheduleAsync(query.ConsultantProfileId, query.SessionTypeId, ct)
            ?? throw new NotFoundException("ConsultantProfile", query.ConsultantProfileId);

        var now          = DateTime.UtcNow;
        var noticeCutoff = now.AddHours(schedule.MinBookingNoticeHours);
        var maxLimit     = now.AddDays(schedule.MaxAdvanceBookingDays);
        var db           = _redis.GetDatabase();
        var slots        = new List<DateTime>();

        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            if (schedule.BlockedDates.Contains(date.ToString("yyyy-MM-dd")))
                continue;

            var dow  = (int)date.DayOfWeek;
            var slot = schedule.WeeklySlots.FirstOrDefault(s => s.DayOfWeek == dow);
            if (slot is null) continue;

            var slotStart = TimeOnly.Parse(slot.StartTimeUtc);
            var slotEnd   = TimeOnly.Parse(slot.EndTimeUtc);

            var candidate = date.ToDateTime(slotStart, DateTimeKind.Utc);
            var endBound  = date.ToDateTime(slotEnd,   DateTimeKind.Utc).AddMinutes(-schedule.DurationMinutes);

            while (candidate <= endBound)
            {
                if (candidate >= noticeCutoff && candidate <= maxLimit)
                {
                    var lockKey = $"slot:{query.ConsultantProfileId}:{candidate:yyyyMMddHHmm}";
                    var locked  = await db.KeyExistsAsync(lockKey);
                    if (!locked && !await _bookingServices.IsSlotTakenAsync(query.ConsultantProfileId, candidate, ct))
                        slots.Add(candidate);
                }
                candidate = candidate.AddMinutes(schedule.DurationMinutes);
            }
        }

        return slots;
    }
}
