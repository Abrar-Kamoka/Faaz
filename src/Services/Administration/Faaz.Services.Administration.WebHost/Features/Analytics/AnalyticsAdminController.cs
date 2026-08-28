using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.Services.Administration.WebHost.Features.AuditLog;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Analytics;

[Route("api/v1/admin/analytics")]
[Authorize(Policy = "AdminOnly")]
public class AnalyticsAdminController(
    IAdminBookingClient bookingClient,
    IAdminPaymentClient paymentClient,
    IAdminIdentityClient identityClient,
    IAdminActionLogServices auditLog,
    IAuditLogEnricher enricher) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var bookingAnalytics = await bookingClient.GetAnalyticsAsync(from, to, ct);

        var (auditLogs, auditTotal) = await auditLog.GetAllAsync(1, 10, ct: ct);
        var enrichedAuditLogs = await enricher.EnrichAsync(auditLogs, ct);

        return Ok(ApiResponse.Ok(new
        {
            Bookings         = bookingAnalytics,
            RecentAdminActions = new { Items = enrichedAuditLogs, TotalCount = auditTotal }
        }));
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
    {
        var timeSeries     = await paymentClient.GetRevenueTimeSeriesAsync(from, to, ct) ?? [];
        var topConsultants = await paymentClient.GetTopConsultantsAsync(from, to, 10, ct) ?? [];

        // Best-effort name enrichment — a failed lookup just falls back to a truncated id rather
        // than failing the whole leaderboard.
        var leaderboard = new List<object>();
        foreach (var c in topConsultants)
        {
            var user = await identityClient.GetUserByIdAsync(c.ConsultantUserId, ct);
            leaderboard.Add(new
            {
                c.ConsultantUserId,
                ConsultantName = user is not null ? $"{user.FirstName} {user.LastName}" : c.ConsultantUserId.ToString()[..8],
                c.TotalEarningsGbp,
                c.BookingCount
            });
        }

        return Ok(ApiResponse.Ok(new { RevenueTimeSeries = timeSeries, TopConsultants = leaderboard }));
    }
}
