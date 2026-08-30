using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Internal;

[Route("internal/admin")]
[ApiController]
[AllowAnonymous]
public class BookingAdminInternalController : ControllerBase
{
    private readonly IBookingServices _bookingServices;
    private readonly IBookingIdentityClient _identityClient;
    private readonly IConfiguration _config;

    public BookingAdminInternalController(IBookingServices bookingServices, IBookingIdentityClient identityClient, IConfiguration config)
    {
        _bookingServices = bookingServices;
        _identityClient  = identityClient;
        _config          = config;
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] Guid? consultantId = null,
        [FromQuery] Guid? studentId = null,
        CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (items, total) = await _bookingServices.GetForAdminAsync(page, pageSize, status, consultantId, studentId, ct);

        // Booking doesn't own user profile data — best-effort enrichment from Identity, one lookup per
        // distinct user id on the page; a failed lookup just leaves that name blank.
        var userIds = items.Select(b => b.StudentUserId).Concat(items.Select(b => b.ConsultantUserId)).Distinct().ToList();
        var names   = await ResolveNamesAsync(userIds, ct);

        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(b => new
            {
                b.Id,
                b.StudentUserId,
                StudentName    = names.GetValueOrDefault(b.StudentUserId, string.Empty),
                b.ConsultantUserId,
                ConsultantName = names.GetValueOrDefault(b.ConsultantUserId, string.Empty),
                b.SessionTypeName,
                Status         = (int)b.Status,
                AmountGbp      = b.TotalChargedGbp,
                PlatformFeeGbp = b.PlatformCommissionGbp,
                b.ScheduledStartUtc,
                ScheduledEndUtc = b.ScheduledEndUtc,
                CreatedAt       = b.CreatedAt ?? DateTime.MinValue,
                DisputeReason   = b.DisputeReason
            }),
            TotalCount = total
        }));
    }

    [HttpGet("bookings/{bookingId:guid}")]
    public async Task<IActionResult> GetBooking(Guid bookingId, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var b = await _bookingServices.GetByIdWithDetailsAsync(bookingId, ct);
        if (b is null) return NotFound(ApiResponse.Fail(404, "Booking not found."));

        var names = await ResolveNamesAsync([b.StudentUserId, b.ConsultantUserId], ct);

        return Ok(ApiResponse.Ok(new
        {
            b.Id,
            Reference      = b.Id.ToString("N")[..8].ToUpper(),
            b.StudentUserId,
            StudentName    = names.GetValueOrDefault(b.StudentUserId, string.Empty),
            b.ConsultantUserId,
            ConsultantName = names.GetValueOrDefault(b.ConsultantUserId, string.Empty),
            b.SessionTypeName,
            Status         = (int)b.Status,
            AmountGbp      = b.TotalChargedGbp,
            PlatformFeeGbp = b.PlatformCommissionGbp,
            b.ScheduledStartUtc,
            ScheduledEndUtc = b.ScheduledEndUtc,
            CreatedAt             = b.CreatedAt ?? DateTime.MinValue,
            DisputeReason         = b.DisputeReason,
            DisputeResolution     = b.DisputeResolution,
            DisputeResolutionNote = b.DisputeResolutionNote,
            DisputeResolvedAt     = b.DisputeResolvedAt,
            // The student's pre-session message and the consultant's own session notes — both
            // private to the two participants otherwise (see SessionsController's ownership
            // checks); admin gets read access here for oversight/dispute-resolution purposes.
            SessionBrief          = b.SessionBrief,
            SessionNotes          = b.SessionNotes
        }));
    }

    private async Task<Dictionary<Guid, string>> ResolveNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var distinctIds = userIds.Distinct().ToList();
        var lookups      = await Task.WhenAll(distinctIds.Select(id => _identityClient.GetUserNameAsync(id, ct)));

        var result = new Dictionary<Guid, string>();
        for (var i = 0; i < distinctIds.Count; i++)
        {
            if (lookups[i] is { } name) result[distinctIds[i]] = name.FullName;
        }
        return result;
    }

    [HttpGet("students/{studentId:guid}/summary")]
    public async Task<IActionResult> GetStudentSummary(Guid studentId, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (_, total) = await _bookingServices.GetByStudentIdAsync(studentId, 1, 1, ct);
        return Ok(ApiResponse.Ok(new { TotalBookings = total }));
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _bookingServices.GetAnalyticsAsync(from, to, ct);
        return Ok(ApiResponse.Ok(result));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}
