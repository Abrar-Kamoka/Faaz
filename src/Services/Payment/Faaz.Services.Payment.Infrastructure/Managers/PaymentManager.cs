using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.Managers
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;

    internal sealed class PaymentManager : IPaymentServices
    {
        private readonly PaymentDbContext _db;

        public PaymentManager(PaymentDbContext db) { _db = db; }

        public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Payments.Include(x => x.Refunds).FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
            => await _db.Payments.Include(x => x.Refunds).FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);

        public async Task<Payment?> GetByStripePaymentIntentIdAsync(string intentId, CancellationToken ct = default)
            => await _db.Payments.FirstOrDefaultAsync(x => x.StripePaymentIntentId == intentId, ct);

        public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByConsultantAsync(
            Guid consultantUserId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Payments
                .Where(x => x.ConsultantUserId == consultantUserId)
                .OrderByDescending(x => x.CreatedAt);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task AddAsync(Payment payment, CancellationToken ct = default)
            => await _db.Payments.AddAsync(payment, ct);

        public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
        {
            var max = await _db.Payments.MaxAsync(x => (int?)x.SrNo, ct);
            return (max ?? 0) + 1;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);
    }
}
