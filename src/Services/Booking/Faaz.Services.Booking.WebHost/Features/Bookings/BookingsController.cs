using Faaz.Services.Booking.WebHost.Features.Bookings.Commands;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.Services.Booking.WebHost.Features.Bookings.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> RescheduleBooking(Guid id, [FromBody] RescheduleBookingDto dto)
    {
        await _mediator.Send(new RescheduleBookingCommand
        {
            BookingId           = id,
            RequestingStudentId = GetUserId(),
            PostModel            = dto
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

    [HttpPost("{id:guid}/dispute")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> FileDispute(Guid id, [FromBody] FileDisputeDto dto)
    {
        await _mediator.Send(new FileDisputeCommand
        {
            BookingId           = id,
            RequestingStudentId = GetUserId(),
            PostModel            = dto
        });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPost("{id:guid}/resolve-dispute")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeDto dto)
    {
        await _mediator.Send(new ResolveDisputeCommand
        {
            BookingId   = id,
            AdminUserId = GetUserId(),
            PostModel    = dto
        });
        return Ok(ApiResponse.NoContent());
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> OverrideStatus(Guid id, [FromBody] OverrideStatusDto dto)
    {
        await _mediator.Send(new OverrideBookingStatusCommand
        {
            BookingId   = id,
            AdminUserId = GetUserId(),
            PostModel    = dto
        });
        return Ok(ApiResponse.NoContent());
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

    [HttpGet("{id:guid}/invoice")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        var booking = await _mediator.Send(new GetBookingByIdQuery
        {
            BookingId        = id,
            RequestingUserId = GetUserId(),
            RequestingRole   = GetRole()
        });

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FAAZ").FontSize(28).Bold().FontColor("#1A9E5C");
                        col.Item().Text("Consultation Invoice").FontSize(14).FontColor("#555555");
                    });
                    row.ConstantItem(160).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Invoice #{booking.SrNo:D6}").Bold();
                        col.Item().AlignRight().Text($"Date: {booking.ScheduledStartUtc:dd MMM yyyy}").FontColor("#555555");
                    });
                });

                page.Content().PaddingTop(30).Column(col =>
                {
                    col.Item().Background("#F8F6F2").Padding(16).Column(details =>
                    {
                        details.Item().Text("Booking Details").FontSize(13).Bold();
                        details.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Session: {booking.SessionTypeName}");
                                c.Item().Text($"Date: {booking.ScheduledStartUtc:dddd, dd MMMM yyyy}");
                                c.Item().Text($"Time: {booking.ScheduledStartUtc:HH:mm} UTC");
                                c.Item().Text($"Duration: {booking.DurationMinutes} minutes");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Format: {booking.CallType}");
                                c.Item().Text($"Status: {booking.Status}");
                                c.Item().Text($"Booking Ref: {booking.Id.ToString()[..8].ToUpperInvariant()}");
                            });
                        });
                    });

                    col.Item().PaddingTop(24).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1A9E5C").Padding(8).Text("Description").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#1A9E5C").Padding(8).Text("Qty").FontColor("#FFFFFF").Bold().AlignCenter();
                            header.Cell().Background("#1A9E5C").Padding(8).Text("Amount").FontColor("#FFFFFF").Bold().AlignRight();
                        });

                        table.Cell().BorderBottom(1).BorderColor("#E0DBD0").Padding(8).Text(booking.SessionTypeName);
                        table.Cell().BorderBottom(1).BorderColor("#E0DBD0").Padding(8).Text("1").AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor("#E0DBD0").Padding(8).Text($"£{booking.SessionPriceGbp:F2}").AlignRight();

                        table.Cell().ColumnSpan(2).Padding(8).Text("Total").Bold();
                        table.Cell().Padding(8).Text($"£{booking.TotalChargedGbp:F2}").Bold().AlignRight();
                    });

                    col.Item().PaddingTop(32).Text("Thank you for using Faaz. If you have any questions about this invoice, please contact support@faaz.co.uk")
                        .FontSize(10).FontColor("#A0A09C").AlignCenter();
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Faaz Ltd · support@faaz.co.uk · faaz.co.uk").FontSize(9).FontColor("#A0A09C");
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return File(bytes, "application/pdf", $"faaz-invoice-{booking.SrNo:D6}.pdf");
    }
}
