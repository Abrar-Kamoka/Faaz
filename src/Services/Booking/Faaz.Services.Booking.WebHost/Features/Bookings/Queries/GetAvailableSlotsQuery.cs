using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Queries;

public class GetAvailableSlotsQuery : IRequest<AvailableSlotsResult>
{
    public Guid     ConsultantProfileId { get; set; }
    public Guid     SessionTypeId       { get; set; }
    public DateOnly From                { get; set; }
    public DateOnly To                  { get; set; }
}

// The consultant's own booking-window limits, alongside the computed slots — the caller (student
// slot picker) needs MaxAdvanceBookingDays to size its date range to what's actually configurable per
// consultant instead of a fixed guess, and this handler already has the authoritative value on hand.
public record AvailableSlotsResult(IReadOnlyList<DateTime> Slots, int MinBookingNoticeHours, int MaxAdvanceBookingDays);

internal sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, AvailableSlotsResult>
{
    private readonly IBookingConsultantClient _consultantClient;
    private readonly IBookingServices         _bookingServices;
    private readonly ISlotLockService         _slotLock;

    public GetAvailableSlotsQueryHandler(
        IBookingConsultantClient consultantClient,
        IBookingServices bookingServices,
        ISlotLockService slotLock)
    {
        _consultantClient = consultantClient;
        _bookingServices  = bookingServices;
        _slotLock         = slotLock;
    }

    public async Task<AvailableSlotsResult> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        if ((query.To.DayNumber - query.From.DayNumber) > 60)
            throw BusinessRuleException.Error("Date range must not exceed 60 days.", "slots.range-too-wide");

        var schedule = await _consultantClient.GetConsultantScheduleAsync(query.ConsultantProfileId, query.SessionTypeId, ct)
            ?? throw new NotFoundException("ConsultantProfile", query.ConsultantProfileId);

        var now          = DateTime.UtcNow;
        var noticeCutoff = now.AddHours(schedule.MinBookingNoticeHours);
        var maxLimit     = now.AddDays(schedule.MaxAdvanceBookingDays);
        var slots        = new List<DateTime>();

        // WeeklySlots/BlockedDates are wall-clock values in the consultant's own timezone, not UTC —
        // a fixed UTC offset can't represent a recurring weekly slot correctly once DST is involved,
        // since the true offset shifts twice a year in any zone that observes it. Resolve per date
        // instead of precomputing a single offset.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);

        // Padded a day on each side: [From, To] is expressed in the caller's own calendar days, which
        // can differ from the consultant's local calendar day near midnight once the two timezones
        // are far apart. Without this, a real slot right at the edge of the range could be silently
        // dropped rather than just filtered out downstream by whichever calendar the caller displays.
        for (var date = query.From.AddDays(-1); date <= query.To.AddDays(1); date = date.AddDays(1))
        {
            if (schedule.BlockedDates.Contains(date.ToString("yyyy-MM-dd")))
                continue;

            var dow  = (int)date.DayOfWeek;
            var slot = schedule.WeeklySlots.FirstOrDefault(s => s.DayOfWeek == dow);
            if (slot is null) continue;

            var slotStart = TimeOnly.Parse(slot.StartTimeLocal);
            var slotEnd   = TimeOnly.Parse(slot.EndTimeLocal);

            var localCandidateStart = date.ToDateTime(slotStart, DateTimeKind.Unspecified);
            var localCandidateEnd   = date.ToDateTime(slotEnd,   DateTimeKind.Unspecified);

            // A "spring forward" DST transition can mean this wall-clock time never occurred on this
            // date at all — skip it rather than let TimeZoneInfo throw and take the whole request down.
            if (tz.IsInvalidTime(localCandidateStart) || tz.IsInvalidTime(localCandidateEnd))
                continue;

            var candidate = TimeZoneInfo.ConvertTimeToUtc(localCandidateStart, tz);
            var endBound  = TimeZoneInfo.ConvertTimeToUtc(localCandidateEnd, tz).AddMinutes(-schedule.DurationMinutes);

            while (candidate <= endBound)
            {
                if (candidate >= noticeCutoff && candidate <= maxLimit)
                {
                    var lockKey = $"slot:{query.ConsultantProfileId}:{candidate:yyyyMMddHHmm}";
                    var locked  = await _slotLock.ExistsAsync(lockKey, ct);
                    if (!locked && !await _bookingServices.IsSlotTakenAsync(query.ConsultantProfileId, candidate, ct))
                        slots.Add(candidate);
                }
                candidate = candidate.AddMinutes(schedule.DurationMinutes);
            }
        }

        return new AvailableSlotsResult(slots, schedule.MinBookingNoticeHours, schedule.MaxAdvanceBookingDays);
    }
}
