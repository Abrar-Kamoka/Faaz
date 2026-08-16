using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Students;

[Route("api/v1/admin/students")]
[Authorize(Policy = "AdminOnly")]
public class StudentsAdminController(
    IAdminIdentityClient identityClient,
    IAdminBookingClient bookingClient,
    IAdminPaymentClient paymentClient) : FaazApiController
{
    // "1" = Student role — mirrors the numeric role convention used by Identity's internal filter.
    private const string StudentRole = "1";

    [HttpGet]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var users = await identityClient.GetUsersAsync(page, pageSize, search, StudentRole, null, ct);
        if (users is null)
            return Problem("Identity service unavailable", statusCode: 503);

        // Best-effort enrichment from Booking/Payment — a failed lookup for one student shouldn't
        // fail the whole page, just leave that student's stats at zero.
        var items = await Task.WhenAll(users.Items.Select(async u =>
        {
            var bookingCountTask = bookingClient.GetStudentBookingCountAsync(u.Id, ct);
            var totalSpentTask   = paymentClient.GetStudentTotalSpentAsync(u.Id, ct);
            await Task.WhenAll(bookingCountTask, totalSpentTask);
            return new AdminStudentSummary(
                u.Id, u.Email, u.FirstName, u.LastName, u.IsActive, u.CreatedAt,
                bookingCountTask.Result, totalSpentTask.Result);
        }));

        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = users.TotalCount }));
    }
}

public record AdminStudentSummary(
    Guid Id, string Email, string FirstName, string LastName, bool IsActive, DateTime CreatedAt,
    int TotalBookings, decimal TotalSpentGbp);
