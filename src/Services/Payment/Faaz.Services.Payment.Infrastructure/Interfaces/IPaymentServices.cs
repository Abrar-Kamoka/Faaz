namespace Faaz.Services.Payment.Infrastructure.Interfaces
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;

    public interface IPaymentServices
    {
        Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
        Task<Payment?> GetByStripePaymentIntentIdAsync(string intentId, CancellationToken ct = default);
        Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByConsultantAsync(Guid consultantUserId, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Payment payment, CancellationToken ct = default);
        Task<int> NewSerialNumberAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
