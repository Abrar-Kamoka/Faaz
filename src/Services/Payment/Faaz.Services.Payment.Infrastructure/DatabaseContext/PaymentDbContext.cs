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
        }
    }
}
