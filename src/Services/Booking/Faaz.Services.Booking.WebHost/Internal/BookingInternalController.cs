using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Internal;

[Route("internal/booking")]
[ApiController]
[AllowAnonymous]
public class BookingInternalController : ControllerBase
{
    private readonly IBookingServices _bookingServices;

    public BookingInternalController(IBookingServices b) { _bookingServices = b; }

    [HttpGet("slot-availability")]
    public async Task<IActionResult> CheckSlotAvailability(
        [FromQuery] Guid consultantProfileId,
        [FromQuery] DateTime slotStartUtc,
        CancellationToken ct)
    {
        var isTaken = await _bookingServices.IsSlotTakenAsync(consultantProfileId, slotStartUtc, ct);
        return Ok(ApiResponse.Ok(new { Available = !isTaken }));
    }

    [HttpGet("{bookingId:guid}/details")]
    public async Task<IActionResult> GetBookingDetails(Guid bookingId, CancellationToken ct)
    {
        var result = await _bookingServices.GetBookingPaymentDetailsAsync(bookingId, ct);
        if (result is null) return NotFound();
        return Ok(ApiResponse.Ok(result));
    }
}
