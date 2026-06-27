using Faaz.Services.Booking.WebHost.Features.Reviews.Commands;
using Faaz.Services.Booking.WebHost.Features.Reviews.DTOs;
using Faaz.Services.Booking.WebHost.Features.Reviews.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Features.Reviews;

[Route("api/reviews")]
public class ReviewsController : FaazApiController
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("{bookingId:guid}")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> SubmitReview(Guid bookingId, [FromBody] SubmitReviewDto dto)
    {
        var reviewId = await _mediator.Send(new SubmitReviewCommand
        {
            BookingId        = bookingId,
            RequestingUserId = GetUserId(),
            PostModel        = dto
        });
        return StatusCode(201, ApiResponse.Created(new { Id = reviewId }));
    }

    [HttpGet("consultant/{profileId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConsultantReviews(Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _mediator.Send(new GetConsultantReviewsQuery
        {
            ConsultantProfileId = profileId,
            Page                = page,
            PageSize            = pageSize
        });
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("consultant/{profileId:guid}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewSummary(Guid profileId)
    {
        var result = await _mediator.Send(new GetReviewSummaryQuery { ConsultantProfileId = profileId });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPatch("admin/{reviewId:guid}/visibility")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetVisibility(Guid reviewId, [FromBody] SetVisibilityDto dto)
    {
        await _mediator.Send(new SetReviewVisibilityCommand
        {
            ReviewId    = reviewId,
            AdminUserId = GetUserId(),
            IsPublic    = dto.IsPublic
        });
        return Ok(ApiResponse.NoContent());
    }
}
