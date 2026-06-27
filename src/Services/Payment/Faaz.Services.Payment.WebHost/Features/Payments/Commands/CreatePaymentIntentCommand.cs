using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Faaz.Services.Payment.Infrastructure.Services;
using Faaz.Services.Payment.WebHost.Features.Payments.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;

namespace Faaz.Services.Payment.WebHost.Features.Payments.Commands
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;
    using static global::Faaz.Services.Payment.Domain.PaymentEnums;

    public class CreatePaymentIntentCommand : IRequest<PaymentIntentResultDto>
    {
        public Guid StudentUserId { get; set; }
        public CreatePaymentIntentDto PostModel { get; set; } = null!;
    }

    public class CreatePaymentIntentCommandHandler : IRequestHandler<CreatePaymentIntentCommand, PaymentIntentResultDto>
    {
        private readonly IPaymentServices _paymentServices;
        private readonly IPromoCodeServices _promoServices;
        private readonly IPaymentGateway _gateway;
        private readonly IPaymentConsultantClient _consultantClient;
        private readonly IBookingClient _bookingClient;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreatePaymentIntentCommandHandler(
            IPaymentServices ps, IPromoCodeServices promo, IPaymentGateway gw,
            IPaymentConsultantClient cc, IBookingClient bc, IPublishEndpoint pub)
        { _paymentServices = ps; _promoServices = promo; _gateway = gw; _consultantClient = cc; _bookingClient = bc; _publishEndpoint = pub; }

        public async Task<PaymentIntentResultDto> Handle(CreatePaymentIntentCommand command, CancellationToken ct)
        {
            var dto = command.PostModel;

            var existing = await _paymentServices.GetByBookingIdAsync(dto.BookingId, ct);
            if (existing is not null && existing.Status is PaymentStatus.Authorised or PaymentStatus.Captured)
                throw new ConflictException("A payment already exists for this booking.");

            var bookingData = await _bookingClient.GetBookingDetailsAsync(dto.BookingId, ct)
                ?? throw new NotFoundException("Booking", dto.BookingId);
            decimal bookingAmount    = bookingData.TotalChargedGbp;
            Guid    consultantUserId = bookingData.ConsultantUserId;
            string? stripeCustomerId = null; // no Stripe Customer ID on student profile; null = anonymous intent

            var connectAccountId = await _consultantClient.GetStripeConnectAccountIdAsync(consultantUserId, ct);
            if (string.IsNullOrEmpty(connectAccountId))
                throw BusinessRuleException.Error("Consultant has not connected their Stripe account yet.", "payment.no-connect-account");

            decimal discount    = 0m;
            string? promoUsed   = null;
            if (!string.IsNullOrEmpty(dto.PromoCode))
            {
                var promo = await _promoServices.GetByCodeAsync(dto.PromoCode, ct);
                if (promo is not null && promo.IsActive
                    && (promo.ValidTo is null || promo.ValidTo >= DateTime.UtcNow)
                    && (promo.MaxUses is null || promo.CurrentUses < promo.MaxUses))
                {
                    discount = promo.DiscountType == PromoDiscountType.Percentage
                        ? Math.Round(bookingAmount * (promo.DiscountValue / 100m), 2)
                        : promo.DiscountValue;
                    if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount.Value)
                        discount = promo.MaxDiscountAmount.Value;
                    promoUsed = promo.Code;
                    promo.CurrentUses++;
                    await _promoServices.SaveChangesAsync(ct);
                }
            }

            var finalAmount = Math.Max(bookingAmount - discount, 0m);
            var platformFee = Math.Round(finalAmount * 0.15m, 2);

            var result = await _gateway.CreatePaymentIntentAsync(
                finalAmount, stripeCustomerId, connectAccountId, platformFee, dto.BookingId.ToString(), ct);

            if (!result.Success || result.PaymentIntentId is null)
                throw BusinessRuleException.Error($"Payment creation failed: {result.ErrorMessage}", "payment.intent-failed");

            var srNo = await _paymentServices.NewSerialNumberAsync(ct);
            var payment = new Payment
            {
                SrNo                  = srNo,
                BookingId             = dto.BookingId,
                StudentUserId         = command.StudentUserId,
                ConsultantUserId      = consultantUserId,
                StripePaymentIntentId = result.PaymentIntentId,
                Amount                = finalAmount,
                PlatformFee           = platformFee,
                ConsultantPayout      = finalAmount - platformFee,
                DiscountAmount        = discount,
                PromoCodeUsed         = promoUsed,
                Status                = PaymentStatus.Authorised
            };
            await _paymentServices.AddAsync(payment, ct);
            await _paymentServices.SaveChangesAsync(ct);

            return new PaymentIntentResultDto
            {
                PaymentId    = payment.Id,
                ClientSecret = result.ClientSecret!,
                IntentId     = result.PaymentIntentId,
                Amount       = finalAmount,
                Currency     = "gbp"
            };
        }
    }
}
