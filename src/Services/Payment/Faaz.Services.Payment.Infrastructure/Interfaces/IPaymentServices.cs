namespace Faaz.Services.Payment.Infrastructure.Interfaces
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;

    public record TransactionLedgerEntry(
        Guid Id, Guid BookingId, string Reference, string Type,
        decimal AmountGbp, string Currency, string Status, DateTime CreatedAt);

    // PlatformFeeGbp is the GROSS commission charged (Payment.PlatformFee summed) — it does not
    // subtract Stripe's own processing fee on each transaction, so it overstates actual net platform
    // revenue. True net revenue would need Stripe's per-charge Balance Transaction fee data, which
    // isn't pulled in here.
    public record RevenueDay(DateTime Date, decimal RevenueGbp, decimal PlatformFeeGbp, int PaymentCount);
    public record TopConsultantEarning(Guid ConsultantUserId, decimal TotalEarningsGbp, int BookingCount);

    public interface IPaymentServices
    {
        Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
        Task<Payment?> GetByStripePaymentIntentIdAsync(string intentId, CancellationToken ct = default);
        Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByConsultantAsync(Guid consultantUserId, int page, int pageSize, CancellationToken ct = default);
        Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByStudentAsync(Guid studentUserId, int page, int pageSize, CancellationToken ct = default);
        Task<decimal> GetTotalSpentByStudentAsync(Guid studentUserId, CancellationToken ct = default);
        Task AddAsync(Payment payment, CancellationToken ct = default);
        Task<int> NewSerialNumberAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);

        Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetAllForAdminAsync(
            int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default);

        // Unified admin ledger across Payments, Refunds and Payouts — "type" here means the kind of
        // transaction ("Payment" | "Refund" | "Payout"), not any entity's own Status.
        Task<(IReadOnlyList<TransactionLedgerEntry> Items, int TotalCount)> GetTransactionLedgerForAdminAsync(
            int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default);

        Task<IReadOnlyList<RevenueDay>> GetRevenueTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<IReadOnlyList<TopConsultantEarning>> GetTopConsultantsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default);
    }
}
