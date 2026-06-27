using Faaz.Services.Booking.WebHost.Features.Bookings.Commands;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.Services.Booking.WebHost.Features.Bookings.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Features.Bookings;

[Route("api/bookings")]
public class BookingsController : FaazApiController
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var id = await _mediator.Send(new CreateBookingCommand
        {
            RequestingStudentId = GetUserId(),
            PostModel           = dto
        });
        return StatusCode(201, ApiResponse.Created(new { Id = id }));
    }

    [HttpGet("available-slots")]
    [Authorize]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] GetAvailableSlotsQuery query, CancellationToken ct)
    {
        var slots = await _mediator.Send(query, ct);
        return Ok(ApiResponse.Ok(slots));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var result = await _mediator.Send(new GetBookingByIdQuery
        {
            BookingId        = id,
            RequestingUserId = GetUserId(),
            RequestingRole   = GetRole()
        });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("mine")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _mediator.Send(new GetMyBookingsQuery
        {
            RequestingUserId = GetUserId(),
            RequestingRole   = GetRole(),
            Page             = page,
            PageSize         = pageSize
        });
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> AcceptBooking(Guid id)
    {
        await _mediator.Send(new AcceptBookingCommand { BookingId = id, ConsultantUserId = GetUserId() });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPost("{id:guid}/decline")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> DeclineBooking(Guid id, [FromBody] AdminDecisionDto dto)
    {
        await _mediator.Send(new DeclineBookingCommand
        {
            BookingId                = id,
            RequestingConsultantId   = GetUserId(),
            Notes                    = dto.Reason
        });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> CancelBooking(Guid id, [FromBody] CancelBookingDto dto)
    {
        await _mediator.Send(new CancelBookingCommand
        {
            BookingId        = id,
            RequestingUserId = GetUserId(),
            RequestingRole   = GetRole(),
            PutModel         = dto
        });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPost("{id:guid}/appeal")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> SubmitRefundAppeal(Guid id, [FromBody] SubmitRefundAppealDto dto)
    {
        var appealId = await _mediator.Send(new SubmitRefundAppealCommand
        {
            BookingId           = id,
            RequestingStudentId = GetUserId(),
            PostModel           = dto
        });
        return StatusCode(201, ApiResponse.Created(new { Id = appealId }));
    }

    [HttpGet("{id:guid}/appeal")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyRefundAppeal(Guid id)
    {
        var result = await _mediator.Send(new GetMyRefundAppealQuery
        {
            BookingId           = id,
            RequestingStudentId = GetUserId()
        });
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("admin/appeals/pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPendingAppeals([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, total) = await _mediator.Send(new GetPendingRefundAppealsQuery { Page = page, PageSize = pageSize });
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpPost("admin/appeals/{appealId:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApproveAppeal(Guid appealId, [FromBody] AdminDecisionDto dto)
    {
        await _mediator.Send(new ApproveRefundAppealCommand
        {
            AppealId    = appealId,
            AdminUserId = GetUserId(),
            AdminNotes  = dto.Reason
        });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPost("admin/appeals/{appealId:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RejectAppeal(Guid appealId, [FromBody] AdminDecisionDto dto)
    {
        await _mediator.Send(new RejectRefundAppealCommand
        {
            AppealId    = appealId,
            AdminUserId = GetUserId(),
            AdminNotes  = dto.Reason ?? ""
        });
        return Ok(ApiResponse.NoContent());
    }
}
