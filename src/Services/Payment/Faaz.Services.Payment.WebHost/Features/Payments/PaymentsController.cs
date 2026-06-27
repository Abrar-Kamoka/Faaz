using Faaz.Services.Payment.WebHost.Features.Payments.Commands;
using Faaz.Services.Payment.WebHost.Features.Payments.DTOs;
using Faaz.Services.Payment.WebHost.Features.Payments.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Payment.WebHost.Features.Payments;

[Route("api/payments")]
public class PaymentsController : FaazApiController
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator) { _mediator = mediator; }

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
}
