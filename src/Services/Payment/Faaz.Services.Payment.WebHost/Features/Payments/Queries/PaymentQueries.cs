using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.WebHost.Features.Payments.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Payment.WebHost.Features.Payments.Queries
{
    using static Faaz.Services.Payment.Domain.PaymentEnums;

    public class GetPaymentStatusQuery : IRequest<PaymentStatusDto>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingUserId { get; set; }
    }

    public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, PaymentStatusDto>
    {
        private readonly IPaymentServices _paymentServices;

        public GetPaymentStatusQueryHandler(IPaymentServices p) { _paymentServices = p; }

        public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery query, CancellationToken ct)
        {
            var payment = await _paymentServices.GetByBookingIdAsync(query.BookingId, ct)
                ?? throw new NotFoundException("Payment", query.BookingId);

            if (payment.StudentUserId != query.RequestingUserId && payment.ConsultantUserId != query.RequestingUserId)
                throw new ForbiddenException("You do not have access to this payment.");

            return new PaymentStatusDto
            {
                Id                     = payment.Id,
                BookingId              = payment.BookingId,
                Status                 = payment.Status.ToString(),
                Amount                 = payment.Amount,
                PlatformFee            = payment.PlatformFee,
                ConsultantPayout       = payment.ConsultantPayout,
                StripePaymentIntentId  = payment.StripePaymentIntentId,
                FailureMessage         = payment.FailureMessage,
                CreatedAt              = payment.CreatedAt ?? DateTime.UtcNow
            };
        }
    }

    public class GetStudentPaymentHistoryQuery : IRequest<(IReadOnlyList<PaymentHistoryItemDto> Items, int TotalCount)>
    {
        public Guid StudentUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetStudentPaymentHistoryQueryHandler : IRequestHandler<GetStudentPaymentHistoryQuery, (IReadOnlyList<PaymentHistoryItemDto> Items, int TotalCount)>
    {
        private readonly IPaymentServices _paymentServices;

        public GetStudentPaymentHistoryQueryHandler(IPaymentServices p) { _paymentServices = p; }

        public async Task<(IReadOnlyList<PaymentHistoryItemDto> Items, int TotalCount)> Handle(GetStudentPaymentHistoryQuery query, CancellationToken ct)
        {
            var (payments, total) = await _paymentServices.GetByStudentAsync(query.StudentUserId, query.Page, query.PageSize, ct);
            var dtos = payments.Select(p => new PaymentHistoryItemDto
            {
                Id             = p.Id,
                BookingId      = p.BookingId,
                Amount         = p.Amount,
                DiscountAmount = p.DiscountAmount,
                PromoCodeUsed  = p.PromoCodeUsed,
                Status         = p.Status.ToString(),
                CreatedAt      = p.CreatedAt ?? DateTime.UtcNow
            }).ToList();
            return (dtos, total);
        }
    }

    public class GetConsultantEarningsQuery : IRequest<ConsultantEarningsDto>
    {
        public Guid ConsultantUserId { get; set; }
    }

    public class GetConsultantEarningsQueryHandler : IRequestHandler<GetConsultantEarningsQuery, ConsultantEarningsDto>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IPayoutServices _payoutServices;

        public GetConsultantEarningsQueryHandler(IPaymentServices ps, IPayoutServices po)
        { _paymentServices = ps; _payoutServices = po; }

        public async Task<ConsultantEarningsDto> Handle(GetConsultantEarningsQuery query, CancellationToken ct)
        {
            var (payments, total)  = await _paymentServices.GetByConsultantAsync(query.ConsultantUserId, 1, 10000, ct);
            var captured           = payments.Where(p => p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.Refunded);
            var totalEarnings      = captured.Sum(p => p.ConsultantPayout);

            var (payouts, _) = await _payoutServices.GetByConsultantAsync(query.ConsultantUserId, 1, 10000, ct);
            var paidOut      = payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.Amount);
            var pending      = totalEarnings - paidOut;

            return new ConsultantEarningsDto
            {
                ConsultantUserId = query.ConsultantUserId,
                TotalEarnings    = totalEarnings,
                PendingPayout    = Math.Max(0m, pending),
                TotalSessions    = total
            };
        }
    }

    public class GetConsultantPayoutsQuery : IRequest<(IReadOnlyList<PayoutDto> Items, int TotalCount)>
    {
        public Guid ConsultantUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetConsultantPayoutsQueryHandler : IRequestHandler<GetConsultantPayoutsQuery, (IReadOnlyList<PayoutDto> Items, int TotalCount)>
    {
        private readonly IPayoutServices _payoutServices;

        public GetConsultantPayoutsQueryHandler(IPayoutServices p) { _payoutServices = p; }

        public async Task<(IReadOnlyList<PayoutDto> Items, int TotalCount)> Handle(GetConsultantPayoutsQuery query, CancellationToken ct)
        {
            var (items, total) = await _payoutServices.GetByConsultantAsync(query.ConsultantUserId, query.Page, query.PageSize, ct);
            var dtos = items.Select(p => new PayoutDto
            {
                Id          = p.Id,
                BookingId   = p.BookingId,
                Amount      = p.Amount,
                Status      = p.Status.ToString(),
                ScheduledAt = p.ScheduledReleaseAt,
                ReleasedAt  = p.ReleasedAt,
                CreatedAt   = p.CreatedAt ?? DateTime.UtcNow
            }).ToList();
            return (dtos, total);
        }
    }

    public class ValidatePromoCodeQuery : IRequest<PromoCodeValidationDto>
    {
        public string Code { get; set; } = "";
        public decimal BookingAmount { get; set; }
    }

    public class ValidatePromoCodeQueryHandler : IRequestHandler<ValidatePromoCodeQuery, PromoCodeValidationDto>
    {
        private readonly IPromoCodeServices _promoServices;

        public ValidatePromoCodeQueryHandler(IPromoCodeServices p) { _promoServices = p; }

        public async Task<PromoCodeValidationDto> Handle(ValidatePromoCodeQuery query, CancellationToken ct)
        {
            var promo = await _promoServices.GetByCodeAsync(query.Code, ct);
            if (promo is null || !promo.IsActive)
                return new PromoCodeValidationDto { IsValid = false, InvalidReason = "Promo code not found or inactive." };

            if (promo.ValidTo.HasValue && promo.ValidTo.Value < DateTime.UtcNow)
                return new PromoCodeValidationDto { IsValid = false, Code = query.Code, InvalidReason = "Promo code has expired." };

            if (promo.MaxUses.HasValue && promo.CurrentUses >= promo.MaxUses.Value)
                return new PromoCodeValidationDto { IsValid = false, Code = query.Code, InvalidReason = "Promo code has reached its usage limit." };

            var discount = promo.DiscountType == Faaz.Services.Payment.Domain.PaymentEnums.PromoDiscountType.Percentage
                ? Math.Round(query.BookingAmount * (promo.DiscountValue / 100m), 2)
                : promo.DiscountValue;
            if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                discount = promo.MaxDiscountAmount.Value;

            return new PromoCodeValidationDto
            {
                IsValid       = true,
                Code          = promo.Code,
                DiscountType  = promo.DiscountType.ToString(),
                DiscountValue = promo.DiscountValue,
                DiscountAmount = discount,
                FinalAmount   = Math.Max(query.BookingAmount - discount, 0m)
            };
        }
    }
}
