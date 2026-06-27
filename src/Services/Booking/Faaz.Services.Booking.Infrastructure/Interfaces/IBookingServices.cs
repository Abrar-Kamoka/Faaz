using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;

    public record BookingPaymentDetailsDto(Guid BookingId, Guid StudentUserId, Guid ConsultantUserId, decimal TotalChargedGbp);

    public interface IBookingServices
    {
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
        Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByStudentIdAsync(Guid studentId, int page, int pageSize, CancellationToken ct = default);
        Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByConsultantIdAsync(Guid consultantId, int page, int pageSize, CancellationToken ct = default);
        Task<BookingPaymentDetailsDto?> GetBookingPaymentDetailsAsync(Guid bookingId, CancellationToken ct = default);
        Task<bool> IsSlotTakenAsync(Guid consultantProfileId, DateTime slotStartUtc, CancellationToken ct = default);
        Task<IReadOnlyList<Booking>> GetExpiredUnconfirmedAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Booking>> GetPayoutEligibleAsync(CancellationToken ct = default);
        Task AddAsync(Booking booking, CancellationToken ct = default);
        Task AddStatusHistoryAsync(BookingStatusHistory history, CancellationToken ct = default);
        Task<int> NewSerialNumberAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
