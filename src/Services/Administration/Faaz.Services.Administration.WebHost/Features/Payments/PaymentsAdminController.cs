using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Payments;

[Route("api/v1/admin/payments")]
[Authorize(Policy = "AdminOnly")]
public class PaymentsAdminController(
    IAdminPaymentClient paymentClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await paymentClient.GetTransactionsAsync(page, pageSize, type, from, to, ct);
        if (result is null)
            return Problem("Payment service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<IActionResult> GetTransaction(Guid transactionId, CancellationToken ct = default)
    {
        var result = await paymentClient.GetTransactionByIdAsync(transactionId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "Transaction not found"));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("transactions/{transactionId:guid}/refund")]
    public async Task<IActionResult> RefundTransaction(
        Guid transactionId,
        [FromBody] RefundRequest req,
        CancellationToken ct = default)
    {
        if (RequirePermission("payments.refund") is { } denied) return denied;

        var adminId = GetUserId();
        var ok = await paymentClient.RefundTransactionAsync(transactionId, adminId, req.Reason, ct);
        if (!ok)
            return Problem("Failed to process refund", statusCode: 502);

        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.ProcessRefund,
            EntityType  = "Transaction",
            EntityId    = transactionId,
            Notes       = req.Reason,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Refund processed."));
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await paymentClient.GetPayoutsAsync(page, pageSize, status, ct);
        if (result is null)
            return Problem("Payment service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("payouts/{payoutId:guid}")]
    public async Task<IActionResult> GetPayout(Guid payoutId, CancellationToken ct = default)
    {
        var result = await paymentClient.GetPayoutByIdAsync(payoutId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "Payout not found"));
        return Ok(ApiResponse.Ok(result));
    }
}

public record RefundRequest(string Reason);
