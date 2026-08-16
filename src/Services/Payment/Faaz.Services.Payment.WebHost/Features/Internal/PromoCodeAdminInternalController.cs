using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.WebHost.Features.Internal;

[Route("internal/admin/promo-codes")]
[ApiController]
[AllowAnonymous]
public class PromoCodeAdminInternalController : ControllerBase
{
    private readonly IPromoCodeServices _promoCodes;
    private readonly IConfiguration _config;

    public PromoCodeAdminInternalController(IPromoCodeServices promoCodes, IConfiguration config)
    {
        _promoCodes = promoCodes;
        _config     = config;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var (items, total) = await _promoCodes.GetPagedAsync(page, pageSize, ct);
        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(Map),
            TotalCount = total
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var p = await _promoCodes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Promo code not found."));
        return Ok(ApiResponse.Ok(Map(p)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeReq req, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        if (!Enum.TryParse<PromoDiscountType>(req.DiscountType, true, out var discountType))
            return BadRequest(ApiResponse.Fail(400, "Invalid discount type. Use 'Percentage' or 'FixedAmount'."));

        var normalizedCode = req.Code.Trim().ToUpperInvariant();
        if (await _promoCodes.ExistsByCodeAsync(normalizedCode, ct))
            throw new ConflictException($"Promo code '{normalizedCode}' already exists.");

        var srNo = await _promoCodes.NewSerialNumberAsync(ct);
        var entity = new PromoCode
        {
            SrNo                = srNo,
            Code                = normalizedCode,
            DiscountType        = discountType,
            DiscountValue       = req.DiscountValue,
            MaxDiscountAmount   = req.MaxDiscountAmount,
            MaxUses             = req.MaxUses,
            ValidFrom           = req.ValidFrom,
            ValidTo             = req.ValidTo,
            Description         = req.Description,
            ConsultantProfileId = req.ConsultantProfileId,
            IsActive            = true
        };
        await _promoCodes.AddAsync(entity, ct);
        await _promoCodes.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(Map(entity)));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromoCodeReq req, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var p = await _promoCodes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Promo code not found."));

        p.DiscountValue     = req.DiscountValue;
        p.MaxDiscountAmount = req.MaxDiscountAmount;
        p.MaxUses           = req.MaxUses;
        p.ValidFrom         = req.ValidFrom;
        p.ValidTo           = req.ValidTo;
        p.Description       = req.Description;
        p.IsActive          = req.IsActive;
        await _promoCodes.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Promo code updated."));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var p = await _promoCodes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Promo code not found."));

        p.IsActive = false;
        await _promoCodes.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Promo code deactivated."));
    }

    private static object Map(PromoCode p) => new
    {
        p.Id,
        p.Code,
        DiscountType        = p.DiscountType.ToString(),
        p.DiscountValue,
        p.MaxDiscountAmount,
        p.MaxUses,
        p.CurrentUses,
        p.ValidFrom,
        p.ValidTo,
        p.IsActive,
        p.Description,
        p.ConsultantProfileId,
        CreatedAt           = p.CreatedAt ?? DateTime.MinValue
    };

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record CreatePromoCodeReq(
    string Code, string DiscountType, decimal DiscountValue,
    decimal? MaxDiscountAmount, int? MaxUses,
    DateTime? ValidFrom, DateTime? ValidTo,
    string? Description, Guid? ConsultantProfileId);

public record UpdatePromoCodeReq(
    decimal DiscountValue, decimal? MaxDiscountAmount, int? MaxUses,
    DateTime? ValidFrom, DateTime? ValidTo,
    string? Description, bool IsActive);
