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

        public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByStudentAsync(
            Guid studentUserId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Payments
                .Where(x => x.StudentUserId == studentUserId)
                .OrderByDescending(x => x.CreatedAt);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task<decimal> GetTotalSpentByStudentAsync(Guid studentUserId, CancellationToken ct = default)
        {
            // "Spent" = money actually captured, regardless of any later (partial) refund —
            // refunds are their own ledger entry, not a reduction of what was originally charged.
            var capturedStatuses = new[]
            {
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Captured,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Refunded,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.PartialRefund
            };
            return await _db.Payments
                .Where(x => x.StudentUserId == studentUserId && capturedStatuses.Contains(x.Status))
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
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

        public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetAllForAdminAsync(
            int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var query = _db.Payments.IgnoreQueryFilters().AsQueryable();
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus>(type, true, out var s))
                query = query.Where(x => x.Status == s);
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue)   query = query.Where(x => x.CreatedAt <= to.Value);
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task<(IReadOnlyList<TransactionLedgerEntry> Items, int TotalCount)> GetTransactionLedgerForAdminAsync(
            int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            // A generous per-kind cap, then merge + page in memory — avoids relying on EF Core to
            // translate a 3-way UNION ALL across differently-shaped entities into a single paged query.
            const int fetchCap = 1000;

            var paymentQuery = _db.Payments.IgnoreQueryFilters().AsQueryable();
            var refundQuery  = _db.Refunds.IgnoreQueryFilters().AsQueryable();
            var payoutQuery  = _db.Payouts.IgnoreQueryFilters().AsQueryable();

            if (from.HasValue)
            {
                paymentQuery = paymentQuery.Where(x => x.CreatedAt >= from.Value);
                refundQuery  = refundQuery.Where(x => x.CreatedAt >= from.Value);
                payoutQuery  = payoutQuery.Where(x => x.CreatedAt >= from.Value);
            }
            if (to.HasValue)
            {
                paymentQuery = paymentQuery.Where(x => x.CreatedAt <= to.Value);
                refundQuery  = refundQuery.Where(x => x.CreatedAt <= to.Value);
                payoutQuery  = payoutQuery.Where(x => x.CreatedAt <= to.Value);
            }

            var entries = new List<TransactionLedgerEntry>();

            if (type is null or "Payment")
            {
                var rows = await paymentQuery.OrderByDescending(x => x.CreatedAt).Take(fetchCap).ToListAsync(ct);
                entries.AddRange(rows.Select(x => new TransactionLedgerEntry(
                    x.Id, x.BookingId, x.StripePaymentIntentId ?? "", "Payment",
                    x.Amount, x.Currency, x.Status.ToString(), x.CreatedAt ?? DateTime.MinValue)));
            }
            if (type is null or "Refund")
            {
                var rows = await refundQuery.OrderByDescending(x => x.CreatedAt).Take(fetchCap).ToListAsync(ct);
                entries.AddRange(rows.Select(x => new TransactionLedgerEntry(
                    x.Id, x.BookingId, x.StripeRefundId ?? "", "Refund",
                    x.Amount, x.Currency, x.Status.ToString(), x.CreatedAt ?? DateTime.MinValue)));
            }
            if (type is null or "Payout")
            {
                var rows = await payoutQuery.OrderByDescending(x => x.CreatedAt).Take(fetchCap).ToListAsync(ct);
                entries.AddRange(rows.Select(x => new TransactionLedgerEntry(
                    x.Id, x.BookingId, x.StripeTransferId ?? "", "Payout",
                    x.Amount, x.Currency, x.Status.ToString(), x.CreatedAt ?? DateTime.MinValue)));
            }

            var ordered = entries.OrderByDescending(x => x.CreatedAt).ToList();
            var items   = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, ordered.Count);
        }

        public async Task<IReadOnlyList<RevenueDay>> GetRevenueTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var capturedStatuses = new[]
            {
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Captured,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Refunded,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.PartialRefund
            };

            var grouped = await _db.Payments
                .Where(x => capturedStatuses.Contains(x.Status) && x.CreatedAt >= from && x.CreatedAt <= to)
                .GroupBy(x => x.CreatedAt!.Value.Date)
                .Select(g => new RevenueDay(g.Key, g.Sum(x => x.Amount), g.Sum(x => x.PlatformFee), g.Count()))
                .ToListAsync(ct);

            return grouped.OrderBy(x => x.Date).ToList();
        }

        public async Task<IReadOnlyList<TopConsultantEarning>> GetTopConsultantsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default)
        {
            var capturedStatuses = new[]
            {
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Captured,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.Refunded,
                global::Faaz.Services.Payment.Domain.PaymentEnums.PaymentStatus.PartialRefund
            };

            return await _db.Payments
                .Where(x => capturedStatuses.Contains(x.Status) && x.CreatedAt >= from && x.CreatedAt <= to)
                .GroupBy(x => x.ConsultantUserId)
                .Select(g => new TopConsultantEarning(g.Key, g.Sum(x => x.ConsultantPayout), g.Count()))
                .OrderByDescending(x => x.TotalEarningsGbp)
                .Take(take)
                .ToListAsync(ct);
        }
    }
}
