using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.Services.Payment.WebHost.Features.Payments.Commands;
using Faaz.Services.Payment.WebHost.Features.Payments.DTOs;
using Faaz.Services.Payment.WebHost.Features.Payments.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Faaz.Services.Payment.WebHost.Features.Payments;

[Route("api/payments")]
public class PaymentsController : FaazApiController
{
    private readonly IMediator _mediator;
    private readonly IPaymentConsultantClient _consultantClient;
    private readonly IConfiguration _config;

    public PaymentsController(IMediator mediator, IPaymentConsultantClient consultantClient, IConfiguration config)
    {
        _mediator         = mediator;
        _consultantClient = consultantClient;
        _config           = config;
    }

    [HttpPost("intent")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentDto dto)
    {
        var result = await _mediator.Send(new CreatePaymentIntentCommand
        {
            StudentUserId = GetUserId(),
            PostModel     = dto
        });
        return StatusCode(201, ApiResponse.Created(result));
    }

    [HttpGet("booking/{bookingId:guid}/status")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> GetPaymentStatus(Guid bookingId)
    {
        var result = await _mediator.Send(new GetPaymentStatusQuery
        {
            BookingId        = bookingId,
            RequestingUserId = GetUserId()
        });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("history")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _mediator.Send(new GetStudentPaymentHistoryQuery
        {
            StudentUserId = GetUserId(),
            Page          = page,
            PageSize      = pageSize
        });
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("promo/{code}/validate")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> ValidatePromoCode(string code, [FromQuery] decimal bookingAmount)
    {
        var result = await _mediator.Send(new ValidatePromoCodeQuery { Code = code, BookingAmount = bookingAmount });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("earnings")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> GetMyEarnings()
    {
        var result = await _mediator.Send(new GetConsultantEarningsQuery { ConsultantUserId = GetUserId() });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("payouts")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> GetMyPayouts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _mediator.Send(new GetConsultantPayoutsQuery
        {
            ConsultantUserId = GetUserId(),
            Page             = page,
            PageSize         = pageSize
        });
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpPost("connect/account-link")]
    [Authorize(Policy = "ConsultantSelfService")]
    public async Task<IActionResult> CreateConnectAccountLink([FromBody] CreateConnectAccountLinkDto? dto, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured.");

        var userId = GetUserId();

        // Get or create the Stripe Connect Express account
        var existingAccountId = await _consultantClient.GetStripeConnectAccountIdAsync(userId, ct);

        var accountService = new AccountService();

        // Destination charges (see StripePaymentGateway.CreatePaymentIntentAsync — the PaymentIntent
        // is created on the platform account with transfer_data.destination) still require the
        // connected account to have card_payments active, not just transfers — Stripe's own Connect
        // guidance for "platform charges, then pays out the seller" marketplaces requests both.
        // transfers alone leaves the account under-verified and Stripe rejects the transfer.
        var capabilities = new AccountCapabilitiesOptions
        {
            CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
            Transfers    = new AccountCapabilitiesTransfersOptions { Requested = true }
        };

        string accountId;
        if (string.IsNullOrEmpty(existingAccountId))
        {
            var account = await accountService.CreateAsync(new AccountCreateOptions
            {
                Type         = "express",
                Country      = "GB",
                Email        = User.FindFirst("email")?.Value,
                Capabilities = capabilities
            }, cancellationToken: ct);

            accountId = account.Id;
            await _consultantClient.SetStripeConnectAccountIdAsync(userId, accountId, ct);
        }
        else
        {
            accountId = existingAccountId;

            // Re-request on every link generation — a no-op if already active/requested, but backfills
            // card_payments for accounts created before this capability was added, surfacing whatever
            // extra onboarding requirements Stripe now needs on the very next AccountLink below.
            await accountService.UpdateAsync(accountId, new AccountUpdateOptions { Capabilities = capabilities }, cancellationToken: ct);
        }

        // Build the return / refresh URLs. Callers outside the setup wizard (e.g. Settings) can
        // override both via ReturnPath — only relative app paths are accepted (never an arbitrary
        // external URL) to avoid this becoming an open redirect. Refresh (link expired/invalid)
        // must land on a page that can request a fresh link, so it defaults separately from the
        // wizard's success destination rather than reusing the same default.
        var baseUrl = _config["PublicBaseUrl"] ?? "https://localhost:3000";
        var isValidOverride = dto?.ReturnPath is { } p && p.StartsWith('/');
        var returnUrl  = $"{baseUrl}{(isValidOverride ? dto!.ReturnPath : "/app/consultant/setup/complete")}?stripe_return=1";
        var refreshUrl = $"{baseUrl}{(isValidOverride ? dto!.ReturnPath : "/app/consultant/setup/stripe")}?stripe_refresh=1";

        var linkService = new AccountLinkService();
        var accountLink = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account    = accountId,
            RefreshUrl = refreshUrl,
            ReturnUrl  = returnUrl,
            Type       = "account_onboarding"
        }, cancellationToken: ct);

        return Ok(ApiResponse.Ok(new { url = accountLink.Url }));
    }

    [HttpGet("connect/status")]
    [Authorize(Policy = "ConsultantSelfService")]
    public async Task<IActionResult> GetConnectStatus(CancellationToken ct)
    {
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured.");

        var userId = GetUserId();
        var accountId = await _consultantClient.GetStripeConnectAccountIdAsync(userId, ct);

        if (string.IsNullOrEmpty(accountId))
            return Ok(ApiResponse.Ok(new { isConnected = false, detailsSubmitted = false, chargesEnabled = false }));

        var service = new AccountService();
        var account = await service.GetAsync(accountId, cancellationToken: ct);

        // Reconcile on every on-demand check, not just via the account.updated webhook — closes the
        // race where a consultant returns from Stripe and navigates away before the webhook lands,
        // which would otherwise leave the authoritative (webhook-synced) flag stale for a few seconds.
        await _consultantClient.UpdateStripeConnectAccountStatusAsync(
            accountId, account.DetailsSubmitted, account.ChargesEnabled, ct);

        return Ok(ApiResponse.Ok(new
        {
            isConnected      = true,
            detailsSubmitted = account.DetailsSubmitted,
            chargesEnabled   = account.ChargesEnabled
        }));
    }

    public record CreateConnectAccountLinkDto(string? ReturnPath);
}
