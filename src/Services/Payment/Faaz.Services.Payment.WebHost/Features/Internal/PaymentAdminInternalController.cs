using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.WebHost.Features.Internal;

[Route("internal/admin")]
[ApiController]
[AllowAnonymous]
public class PaymentAdminInternalController : ControllerBase
{
    private readonly IPaymentServices _payments;
    private readonly IPayoutServices _payouts;
    private readonly IRefundServices _refunds;
    private readonly IPaymentGateway _gateway;
    private readonly IConfiguration _config;

    public PaymentAdminInternalController(
        IPaymentServices payments,
        IPayoutServices payouts,
        IRefundServices refunds,
        IPaymentGateway gateway,
        IConfiguration config)
    {
        _payments = payments;
        _payouts  = payouts;
        _refunds  = refunds;
        _gateway  = gateway;
        _config   = config;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (items, total) = await _payments.GetTransactionLedgerForAdminAsync(page, pageSize, type, from, to, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total }));
    }

    [HttpGet("transactions/{transactionId:guid}")]
    public async Task<IActionResult> GetTransaction(Guid transactionId, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var p = await _payments.GetByIdAsync(transactionId, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Transaction not found."));

        return Ok(ApiResponse.Ok(new
        {
            p.Id,
            p.BookingId,
            Reference            = p.StripePaymentIntentId,
            Type                 = "Payment",
            AmountGbp            = p.Amount,
            p.Currency,
            Status               = p.Status.ToString(),
            StripePaymentIntentId = p.StripePaymentIntentId,
            CreatedAt            = p.CreatedAt ?? DateTime.MinValue
        }));
    }

    [HttpPost("transactions/{transactionId:guid}/refund")]
    public async Task<IActionResult> RefundTransaction(
        Guid transactionId,
        [FromBody] AdminRefundBody req,
        CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var payment = await _payments.GetByIdAsync(transactionId, ct);
        if (payment is null) return NotFound(ApiResponse.Fail(404, "Transaction not found."));
        if (payment.Status == PaymentStatus.Refunded)
            return BadRequest(ApiResponse.Fail(400, "Already refunded."));

        if (string.IsNullOrWhiteSpace(payment.StripeChargeId))
            return BadRequest(ApiResponse.Fail(400, "Payment has not been captured yet and cannot be refunded."));

        // Money for this booking has already been transferred out to the consultant's Connect account
        // (see PayoutReleasedConsumer). A plain refund from here would take the money from the platform's
        // own balance a second time while the consultant keeps what they were already paid — the same
        // underlying issue disputes/appeals guard against via their own SettledAt checks. Claw-back in
        // this state needs an explicit Stripe transfer reversal, which is a deliberate manual action, not
        // something this button should trigger silently.
        var existingPayout = await _payouts.GetByBookingIdAsync(payment.BookingId, ct);
        if (existingPayout is { Status: PayoutStatus.Paid })
            return BadRequest(ApiResponse.Fail(400,
                "Cannot refund — the payout for this booking has already been released to the consultant. This needs a manual transfer reversal, not a customer refund."));

        // Default to a full refund of whatever hasn't been refunded yet; admins can specify a smaller
        // goodwill amount instead of only ever being able to refund everything or nothing.
        var alreadyRefunded = payment.Refunds.Where(r => r.Status == RefundStatus.Succeeded).Sum(r => r.Amount);
        var refundableAmount = payment.Amount - alreadyRefunded;
        var amountToRefund = req.Amount ?? refundableAmount;

        if (amountToRefund <= 0 || amountToRefund > refundableAmount)
            return BadRequest(ApiResponse.Fail(400, $"Refund amount must be between 0 and {refundableAmount:0.00} (already refunded: {alreadyRefunded:0.00})."));

        var stripeResult = await _gateway.CreateRefundAsync(payment.StripeChargeId, amountToRefund, req.Reason, ct);
        if (!stripeResult.Success)
            return StatusCode(502, ApiResponse.Fail(502, $"Stripe refund failed: {stripeResult.ErrorMessage}"));

        // Mark payment as refunded/partially refunded; the charge.refunded webhook will create the
        // Refund record itself with the real StripeRefundId once it lands.
        payment.Status = amountToRefund >= refundableAmount ? PaymentStatus.Refunded : PaymentStatus.PartialRefund;
        await _payments.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Refund issued via Stripe."));
    }

    [HttpGet("analytics/revenue-timeseries")]
    public async Task<IActionResult> GetRevenueTimeSeries(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var result = await _payments.GetRevenueTimeSeriesAsync(from, to, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("analytics/top-consultants")]
    public async Task<IActionResult> GetTopConsultants(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int take = 10, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var result = await _payments.GetTopConsultantsAsync(from, to, take, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("students/{studentId:guid}/summary")]
    public async Task<IActionResult> GetStudentSummary(Guid studentId, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var totalSpent = await _payments.GetTotalSpentByStudentAsync(studentId, ct);
        return Ok(ApiResponse.Ok(new { TotalSpentGbp = totalSpent }));
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (items, total) = await _payouts.GetAllForAdminAsync(page, pageSize, status, ct);
        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(p => new
            {
                p.Id,
                p.ConsultantUserId,
                ConsultantName   = string.Empty,
                AmountGbp        = p.Amount,
                Status           = p.Status.ToString(),
                p.StripeTransferId,
                CreatedAt        = p.CreatedAt ?? DateTime.MinValue,
                ProcessedAt      = p.ReleasedAt
            }),
            TotalCount = total
        }));
    }

    [HttpGet("payouts/{payoutId:guid}")]
    public async Task<IActionResult> GetPayout(Guid payoutId, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var p = await _payouts.GetByIdAsync(payoutId, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Payout not found."));

        return Ok(ApiResponse.Ok(new
        {
            p.Id,
            p.ConsultantUserId,
            ConsultantName   = string.Empty,
            AmountGbp        = p.Amount,
            Status           = p.Status.ToString(),
            p.StripeTransferId,
            CreatedAt        = p.CreatedAt ?? DateTime.MinValue,
            ProcessedAt      = p.ReleasedAt
        }));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record AdminRefundBody(Guid AdminId, string Reason, decimal? Amount = null);
