using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.PromoCodes;

[Route("api/v1/admin/promo-codes")]
[Authorize(Policy = "AdminOnly")]
public class PromoCodesAdminController(
    IAdminPaymentClient paymentClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await paymentClient.GetPromoCodesAsync(page, pageSize, ct);
        if (result is null) return StatusCode(502, ApiResponse.Fail(502, "Payment service unavailable."));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await paymentClient.GetPromoCodeByIdAsync(id, ct);
        if (result is null) return NotFound(ApiResponse.Fail(404, "Promo code not found."));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeBody req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var result  = await paymentClient.CreatePromoCodeAsync(req, ct);
        if (result is null) return StatusCode(502, ApiResponse.Fail(502, "Failed to create promo code."));

        await LogAsync(adminId, AdminAction.CreatePromoCode, result.Id, req.Code, ct);

        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromoCodeBody req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok      = await paymentClient.UpdatePromoCodeAsync(id, req, ct);
        if (!ok) return StatusCode(502, ApiResponse.Fail(502, "Failed to update promo code."));

        await LogAsync(adminId, AdminAction.UpdatePromoCode, id, $"Id={id}", ct);

        return Ok(ApiResponse.NoContent("Promo code updated."));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok      = await paymentClient.DeactivatePromoCodeAsync(id, ct);
        if (!ok) return StatusCode(502, ApiResponse.Fail(502, "Failed to deactivate promo code."));

        await LogAsync(adminId, AdminAction.DeactivatePromoCode, id, $"Id={id}", ct);

        return Ok(ApiResponse.NoContent("Promo code deactivated."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "PromoCode",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}
