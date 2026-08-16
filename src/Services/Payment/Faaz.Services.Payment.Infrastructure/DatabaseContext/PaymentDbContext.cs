using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.DatabaseContext
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;

    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Payment>             Payments             { get; set; }
        public DbSet<Refund>              Refunds              { get; set; }
        public DbSet<Payout>              Payouts              { get; set; }
        public DbSet<PromoCode>           PromoCodes           { get; set; }
        public DbSet<StripeWebhookEvent>  StripeWebhookEvents  { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("payment");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

            modelBuilder.Entity<Payment>().ApplyStandardColumnOrder(
                nameof(Payment.BookingId), nameof(Payment.StudentUserId), nameof(Payment.ConsultantUserId),
                nameof(Payment.Amount), nameof(Payment.PlatformFee), nameof(Payment.ConsultantPayout), nameof(Payment.DiscountAmount),
                nameof(Payment.Currency), nameof(Payment.PromoCodeUsed), nameof(Payment.Status), nameof(Payment.StripePaymentIntentId),
                nameof(Payment.StripeChargeId), nameof(Payment.StripeCustomerId), nameof(Payment.FailureMessage), nameof(Payment.Metadata));

            modelBuilder.Entity<Payout>().ApplyStandardColumnOrder(
                nameof(Payout.BookingId), nameof(Payout.ConsultantUserId), nameof(Payout.Amount), nameof(Payout.Currency),
                nameof(Payout.Status), nameof(Payout.ScheduledReleaseAt), nameof(Payout.ReleasedAt), nameof(Payout.StripeConnectAccountId),
                nameof(Payout.StripeTransferId), nameof(Payout.FailureReason));

            modelBuilder.Entity<PromoCode>().ApplyStandardColumnOrder(
                nameof(PromoCode.Code), nameof(PromoCode.DiscountType), nameof(PromoCode.DiscountValue), nameof(PromoCode.MaxDiscountAmount),
                nameof(PromoCode.MaxUses), nameof(PromoCode.CurrentUses), nameof(PromoCode.ValidFrom), nameof(PromoCode.ValidTo),
                nameof(PromoCode.IsActive), nameof(PromoCode.ConsultantProfileId), nameof(PromoCode.Description));

            modelBuilder.Entity<Refund>().ApplyStandardColumnOrder(
                nameof(Refund.PaymentId), nameof(Refund.BookingId), nameof(Refund.StudentUserId), nameof(Refund.Amount),
                nameof(Refund.Currency), nameof(Refund.Status), nameof(Refund.Reason), nameof(Refund.RefundPercentage),
                nameof(Refund.IsAppealRefund), nameof(Refund.AppealId), nameof(Refund.StripeRefundId), nameof(Refund.FailureReason));

            modelBuilder.Entity<StripeWebhookEvent>().ApplyStandardColumnOrder(
                nameof(StripeWebhookEvent.StripeEventId), nameof(StripeWebhookEvent.EventType), nameof(StripeWebhookEvent.ReceivedAt),
                nameof(StripeWebhookEvent.Processed), nameof(StripeWebhookEvent.ProcessedAt), nameof(StripeWebhookEvent.PayloadJson),
                nameof(StripeWebhookEvent.ErrorMessage));
        }
    }
}
