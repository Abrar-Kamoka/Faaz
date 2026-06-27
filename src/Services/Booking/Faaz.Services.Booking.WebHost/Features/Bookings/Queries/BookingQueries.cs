using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Queries
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class GetBookingByIdQuery : IRequest<BookingDetailDto>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingUserId { get; set; }
        public string RequestingRole { get; set; } = "";
    }

    public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDetailDto>
    {
        private readonly IBookingServices _bookingServices;

        public GetBookingByIdQueryHandler(IBookingServices b) { _bookingServices = b; }

        public async Task<BookingDetailDto> Handle(GetBookingByIdQuery query, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(query.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), query.BookingId);

            var isAdmin      = query.RequestingRole == "3";
            var isStudent    = query.RequestingRole == "1" && booking.StudentUserId == query.RequestingUserId;
            var isConsultant = query.RequestingRole == "2" && booking.ConsultantUserId == query.RequestingUserId;

            if (!isAdmin && !isStudent && !isConsultant)
                throw new ForbiddenException("You do not have access to this booking.");

            return MapToDetail(booking);
        }

        private static BookingDetailDto MapToDetail(Booking b) => new()
        {
            Id                  = b.Id,
            SrNo                = b.SrNo,
            StudentUserId       = b.StudentUserId,
            ConsultantUserId    = b.ConsultantUserId,
            ConsultantProfileId = b.ConsultantProfileId,
            SessionTypeName     = b.SessionTypeName,
            ScheduledStartUtc   = b.ScheduledStartUtc,
            ScheduledEndUtc     = b.ScheduledEndUtc,
            DurationMinutes     = b.DurationMinutes,
            CallType            = b.CallType.ToString(),
            Status              = b.Status.ToString(),
            SessionPriceGbp     = b.SessionPriceGbp,
            TotalChargedGbp     = b.TotalChargedGbp,
            StudentTimezone     = b.StudentTimezone,
            SessionBrief        = b.SessionBrief,
            AcceptedAt          = b.AcceptedAt,
            CompletedAt         = b.CompletedAt,
            SettledAt = b.SettledAt,
            Session   = b.Session is null ? null : new SessionDto
            {
                Id                    = b.Session.Id,
                LiveKitRoomName       = b.Session.LiveKitRoomName,
                Status                = b.Session.Status.ToString(),
                ActualStartUtc        = b.Session.ActualStartUtc,
                ActualEndUtc          = b.Session.ActualEndUtc,
                ActualDurationSeconds = b.Session.ActualDurationSeconds,
                CompletionPct         = b.Session.CompletionPct
            }
        };
    }

    public class GetMyBookingsQuery : IRequest<(IReadOnlyList<BookingListDto> Items, int TotalCount)>
    {
        public Guid RequestingUserId { get; set; }
        public string RequestingRole { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, (IReadOnlyList<BookingListDto> Items, int TotalCount)>
    {
        private readonly IBookingServices _bookingServices;

        public GetMyBookingsQueryHandler(IBookingServices b) { _bookingServices = b; }

        public async Task<(IReadOnlyList<BookingListDto> Items, int TotalCount)> Handle(GetMyBookingsQuery query, CancellationToken ct)
        {
            IReadOnlyList<Booking> items;
            int total;

            if (query.RequestingRole == "1")
                (items, total) = await _bookingServices.GetByStudentIdAsync(query.RequestingUserId, query.Page, query.PageSize, ct);
            else
                (items, total) = await _bookingServices.GetByConsultantIdAsync(query.RequestingUserId, query.Page, query.PageSize, ct);

            var dtos = items.Select(b => new BookingListDto
            {
                Id                = b.Id,
                SrNo              = b.SrNo,
                Status            = b.Status.ToString(),
                SessionTypeName   = b.SessionTypeName,
                DurationMinutes   = b.DurationMinutes,
                SessionPriceGbp   = b.SessionPriceGbp,
                TotalChargedGbp   = b.TotalChargedGbp,
                CallType          = b.CallType.ToString(),
                ScheduledStartUtc = b.ScheduledStartUtc,
                StudentTimezone   = b.StudentTimezone,
                HasReview         = b.Review is not null
            }).ToList();

            return (dtos, total);
        }
    }

    public class GetMyRefundAppealQuery : IRequest<RefundAppealDto?>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingStudentId { get; set; }
    }

    public class GetMyRefundAppealQueryHandler : IRequestHandler<GetMyRefundAppealQuery, RefundAppealDto?>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IRefundAppealServices _appealServices;

        public GetMyRefundAppealQueryHandler(IBookingServices b, IRefundAppealServices a)
        { _bookingServices = b; _appealServices = a; }

        public async Task<RefundAppealDto?> Handle(GetMyRefundAppealQuery query, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(query.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), query.BookingId);

            if (booking.StudentUserId != query.RequestingStudentId)
                throw new ForbiddenException("You do not have access to this booking's appeal.");

            var appeal = await _appealServices.GetByBookingIdAsync(query.BookingId, ct);
            if (appeal is null) return null;

            return new RefundAppealDto
            {
                Id                 = appeal.Id,
                BookingId          = appeal.BookingId,
                Reason             = appeal.Reason,
                RequestedAmountGbp = appeal.RequestedAmountGbp,
                Status             = appeal.Status.ToString(),
                SubmittedAt        = appeal.CreatedAt ?? DateTime.UtcNow
            };
        }
    }

    public class GetPendingRefundAppealsQuery : IRequest<(IReadOnlyList<RefundAppealAdminDto> Items, int TotalCount)>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetPendingRefundAppealsQueryHandler : IRequestHandler<GetPendingRefundAppealsQuery, (IReadOnlyList<RefundAppealAdminDto> Items, int TotalCount)>
    {
        private readonly IRefundAppealServices _appealServices;

        public GetPendingRefundAppealsQueryHandler(IRefundAppealServices a) { _appealServices = a; }

        public async Task<(IReadOnlyList<RefundAppealAdminDto> Items, int TotalCount)> Handle(GetPendingRefundAppealsQuery query, CancellationToken ct)
        {
            var (items, total) = await _appealServices.GetPendingAsync(query.Page, query.PageSize, ct);

            var dtos = items.Select(a => new RefundAppealAdminDto
            {
                Id                 = a.Id,
                BookingId          = a.BookingId,
                StudentUserId      = a.StudentUserId,
                Reason             = a.Reason,
                RequestedAmountGbp = a.RequestedAmountGbp,
                Status             = a.Status.ToString(),
                SubmittedAt        = a.CreatedAt ?? DateTime.UtcNow
            }).ToList();

            return (dtos, total);
        }
    }
}
