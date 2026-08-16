using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Bookings;

[Route("api/v1/admin/bookings")]
[Authorize(Policy = "AdminOnly")]
public class BookingsAdminController(IAdminBookingClient bookingClient) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] Guid? consultantId = null,
        [FromQuery] Guid? studentId = null,
        CancellationToken ct = default)
    {
        var result = await bookingClient.GetBookingsAsync(page, pageSize, status, consultantId, studentId, ct);
        if (result is null)
            return Problem("Booking service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> GetBooking(Guid bookingId, CancellationToken ct = default)
    {
        var result = await bookingClient.GetBookingByIdAsync(bookingId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "Booking not found"));
        return Ok(ApiResponse.Ok(result));
    }
}
