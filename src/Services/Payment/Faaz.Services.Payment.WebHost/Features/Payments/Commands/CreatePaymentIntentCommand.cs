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
        private readonly IConfiguration _config;

        public CreatePaymentIntentCommandHandler(
            IPaymentServices ps, IPromoCodeServices promo, IPaymentGateway gw,
            IPaymentConsultantClient cc, IBookingClient bc, IPublishEndpoint pub, IConfiguration config)
        { _paymentServices = ps; _promoServices = promo; _gateway = gw; _consultantClient = cc; _bookingClient = bc; _publishEndpoint = pub; _config = config; }

        public async Task<PaymentIntentResultDto> Handle(CreatePaymentIntentCommand command, CancellationToken ct)
        {
            var dto = command.PostModel;

            var existing = await _paymentServices.GetByBookingIdAsync(dto.BookingId, ct);
            if (existing is not null && existing.Status == PaymentStatus.Captured)
                throw new ConflictException("A payment already exists for this booking.");

            // Resume an abandoned-but-unpaid checkout (student navigated away before confirming card
            // details) instead of creating a second PaymentIntent and orphaning the first one.
            if (existing is not null && existing.Status == PaymentStatus.Authorised)
            {
                var retrieved = await _gateway.RetrievePaymentIntentAsync(existing.StripePaymentIntentId, ct);
                if (retrieved.Success && retrieved.Status is not ("succeeded" or "canceled"))
                {
                    return new PaymentIntentResultDto
                    {
                        PaymentId        = existing.Id,
                        ClientSecret     = retrieved.ClientSecret!,
                        IntentId         = existing.StripePaymentIntentId,
                        Amount           = existing.Amount,
                        PlatformFee      = existing.PlatformFee,
                        ConsultantPayout = existing.ConsultantPayout,
                        Currency         = "gbp"
                    };
                }
                // Stale local state (intent expired/canceled on Stripe's side, e.g. by the slot-expiry
                // job) — fall through and issue a fresh intent, reusing this same Payment row below.
            }

            var bookingData = await _bookingClient.GetBookingDetailsAsync(dto.BookingId, ct)
                ?? throw new NotFoundException("Booking", dto.BookingId);
            decimal bookingAmount    = bookingData.TotalChargedGbp;
            Guid    consultantUserId = bookingData.ConsultantUserId;
            string? stripeCustomerId = null; // no Stripe Customer ID on student profile; null = anonymous intent

            var connectStatus = await _consultantClient.GetStripeConnectStatusAsync(consultantUserId, ct);
            if (string.IsNullOrEmpty(connectStatus.AccountId))
                throw BusinessRuleException.Error("Consultant has not connected their Stripe account yet.", "payment.no-connect-account");
            if (!connectStatus.ChargesEnabled)
                throw BusinessRuleException.Error("Consultant's payout account setup is incomplete.", "payment.connect-incomplete");
            var connectAccountId = connectStatus.AccountId;

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

            var commissionRate = decimal.TryParse(_config["Stripe:CommissionRate"], out var rate) ? rate : 0.15m;
            var finalAmount    = Math.Max(bookingAmount - discount, 0m);
            var platformFee    = Math.Round(finalAmount * commissionRate, 2);

            var result = await _gateway.CreatePaymentIntentAsync(
                finalAmount, stripeCustomerId, connectAccountId, platformFee, dto.BookingId.ToString(), ct);

            if (!result.Success || result.PaymentIntentId is null)
                throw BusinessRuleException.Error($"Payment creation failed: {result.ErrorMessage}", "payment.intent-failed");

            Payment payment;
            if (existing is not null)
            {
                payment                       = existing;
                payment.StripePaymentIntentId = result.PaymentIntentId;
                payment.Amount                = finalAmount;
                payment.PlatformFee           = platformFee;
                payment.ConsultantPayout      = finalAmount - platformFee;
                payment.DiscountAmount        = discount;
                payment.PromoCodeUsed         = promoUsed;
                payment.Status                = PaymentStatus.Authorised;
            }
            else
            {
                var srNo = await _paymentServices.NewSerialNumberAsync(ct);
                payment = new Payment
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
            }
            await _paymentServices.SaveChangesAsync(ct);

            return new PaymentIntentResultDto
            {
                PaymentId        = payment.Id,
                ClientSecret     = result.ClientSecret!,
                IntentId         = result.PaymentIntentId,
                Amount           = finalAmount,
                PlatformFee      = platformFee,
                ConsultantPayout = payment.ConsultantPayout,
                Currency         = "gbp"
            };
        }
    }
}
