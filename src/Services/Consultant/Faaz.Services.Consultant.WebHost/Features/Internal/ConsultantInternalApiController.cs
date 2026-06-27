using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Faaz.Services.Consultant.WebHost.Features.Internal;

[Route("internal/consultant")]
[Tags("Internal - Consultant API")]
public class ConsultantInternalApiController : ConsultantInternalController
{
    private readonly IConsultantProfileServices _profileServices;

    public ConsultantInternalApiController(IConsultantProfileServices profileServices, IConfiguration config)
        : base(config)
    {
        _profileServices = profileServices;
    }

    [HttpGet("slot-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SlotCheck(
        [FromQuery] Guid profileId,
        [FromQuery] Guid sessionTypeId,
        [FromQuery] DateTime start,
        CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByIdWithCollectionsAsync(profileId, ct);
        if (profile is null || !profile.IsActive)
            return NotFound(ApiResponse.Fail(404, "Consultant profile not found or inactive."));

        var sessionType = profile.SessionTypes.FirstOrDefault(st => st.Id == sessionTypeId && st.IsActive);
        if (sessionType is null)
            return Ok(new { isAvailable = false, consultantUserId = Guid.Empty, sessionTypeName = "", sessionPriceGbp = 0m, durationMinutes = 0 });

        var now    = DateTime.UtcNow;
        var utcStart = start.Kind == DateTimeKind.Utc ? start : DateTime.SpecifyKind(start, DateTimeKind.Utc);

        // MinBookingNoticeHours check
        if ((utcStart - now).TotalHours < profile.MinBookingNoticeHours)
            return Ok(new { isAvailable = false, consultantUserId = Guid.Empty, sessionTypeName = "", sessionPriceGbp = 0m, durationMinutes = 0 });

        // MaxAdvanceBookingDays check
        if ((utcStart - now).TotalDays > profile.MaxAdvanceBookingDays)
            return Ok(new { isAvailable = false, consultantUserId = Guid.Empty, sessionTypeName = "", sessionPriceGbp = 0m, durationMinutes = 0 });

        // Blocked date check
        var slotDate = DateOnly.FromDateTime(utcStart);
        if (profile.AvailabilitySlots.Any(s => s.IsBlockedDate && s.Date == slotDate))
            return Ok(new { isAvailable = false, consultantUserId = Guid.Empty, sessionTypeName = "", sessionPriceGbp = 0m, durationMinutes = 0 });

        // DayOfWeek + time window check
        var slotTime   = TimeOnly.FromDateTime(utcStart);
        var dayOfWeek  = utcStart.DayOfWeek;
        var inWindow   = profile.AvailabilitySlots.Any(s =>
            !s.IsBlockedDate &&
            s.DayOfWeek     == dayOfWeek &&
            s.StartTimeUtc  <= slotTime &&
            s.EndTimeUtc    >= slotTime);

        if (!inWindow)
            return Ok(new { isAvailable = false, consultantUserId = Guid.Empty, sessionTypeName = "", sessionPriceGbp = 0m, durationMinutes = 0 });

        return Ok(new
        {
            isAvailable     = true,
            consultantUserId = profile.UserId,
            sessionTypeName  = sessionType.Name,
            sessionPriceGbp  = sessionType.PriceGbp,
            durationMinutes  = sessionType.DurationMinutes
        });
    }

    [HttpGet("schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] Guid profileId,
        [FromQuery] Guid sessionTypeId,
        CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByIdWithCollectionsAsync(profileId, ct);
        if (profile is null || !profile.IsActive)
            return NotFound(ApiResponse.Fail(404, "Consultant profile not found or inactive."));

        var sessionType = profile.SessionTypes.FirstOrDefault(st => st.Id == sessionTypeId && st.IsActive);
        if (sessionType is null)
            return NotFound(ApiResponse.Fail(404, "Session type not found or inactive."));

        var weeklySlots = profile.AvailabilitySlots
            .Where(s => !s.IsBlockedDate)
            .Select(s => new
            {
                DayOfWeek    = (int)s.DayOfWeek!.Value,
                StartTimeUtc = s.StartTimeUtc!.Value.ToString("HH:mm:ss"),
                EndTimeUtc   = s.EndTimeUtc!.Value.ToString("HH:mm:ss")
            }).ToList();

        var blockedDates = profile.AvailabilitySlots
            .Where(s => s.IsBlockedDate)
            .Select(s => s.Date!.Value.ToString("yyyy-MM-dd"))
            .ToList();

        return Ok(new
        {
            weeklySlots           = weeklySlots,
            blockedDates          = blockedDates,
            durationMinutes       = sessionType.DurationMinutes,
            minBookingNoticeHours = profile.MinBookingNoticeHours,
            maxAdvanceBookingDays = profile.MaxAdvanceBookingDays
        });
    }

    [HttpGet("stripe-account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StripeAccount([FromQuery] Guid userId, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByUserIdAsync(userId, ct);
        if (profile is null)
            return NotFound(ApiResponse.Fail(404, "Consultant profile not found."));

        return Ok(new { stripeConnectAccountId = profile.StripeAccountId });
    }
}
