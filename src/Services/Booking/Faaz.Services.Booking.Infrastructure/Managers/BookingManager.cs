using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.Managers
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;

    internal sealed class BookingManager : IBookingServices
    {
        private readonly BookingDbContext _db;

        public BookingManager(BookingDbContext db)
        {
            _db = db;
        }

        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Bookings
                .Include(x => x.Session)
                    .ThenInclude(s => s!.Participants)
                .Include(x => x.Review)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByStudentIdAsync(
            Guid studentId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Bookings
                .Include(x => x.Review)
                .Where(x => x.StudentUserId == studentId)
                .OrderByDescending(x => x.ScheduledStartUtc);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByConsultantIdAsync(
            Guid consultantId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Bookings
                .Include(x => x.Review)
                .Where(x => x.ConsultantUserId == consultantId)
                .OrderByDescending(x => x.ScheduledStartUtc);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task<BookingPaymentDetailsDto?> GetBookingPaymentDetailsAsync(Guid bookingId, CancellationToken ct = default)
        {
            var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
            return b is null ? null : new BookingPaymentDetailsDto(b.Id, b.StudentUserId, b.ConsultantUserId, b.TotalChargedGbp);
        }

        public async Task<bool> IsSlotTakenAsync(Guid consultantProfileId, DateTime slotStartUtc, CancellationToken ct = default)
        {
            return await _db.Bookings.AnyAsync(x =>
                x.ConsultantProfileId == consultantProfileId &&
                x.ScheduledStartUtc == slotStartUtc &&
                (int)x.Status <= 3, ct); // SlotReserved(0), PendingConfirmation(1), Confirmed(2), InProgress(3)
        }

        public async Task<IReadOnlyList<Booking>> GetPayoutEligibleAsync(CancellationToken ct = default)
        {
            // Payout buffer: 48h after CompletedAt, booking still in Completed status
            var cutoff = DateTime.UtcNow.AddHours(-48);
            return await _db.Bookings
                .Where(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.Completed
                         && x.CompletedAt != null && x.CompletedAt <= cutoff)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Booking>> GetExpiredUnconfirmedAsync(CancellationToken ct = default)
        {
            return await _db.Bookings
                .Where(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.PendingConfirmation
                         && x.ExpiresAt != null && x.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Booking>> GetExpiredReservedSlotsAsync(CancellationToken ct = default)
        {
            return await _db.Bookings
                .Where(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.SlotReserved
                         && x.SlotReservedUntilUtc != null && x.SlotReservedUntilUtc <= DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Booking booking, CancellationToken ct = default)
        {
            await _db.Bookings.AddAsync(booking, ct);
        }

        public async Task AddStatusHistoryAsync(BookingStatusHistory history, CancellationToken ct = default)
        {
            await _db.BookingStatusHistory.AddAsync(history, ct);
        }

        public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
        {
            var max = await _db.Bookings.MaxAsync(x => (int?)x.SrNo, ct);
            return (max ?? 0) + 1;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _db.SaveChangesAsync(ct);
        }

        public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetForAdminAsync(
            int page, int pageSize, int? status, Guid? consultantId, Guid? studentId, CancellationToken ct = default)
        {
            var query = _db.Bookings.IgnoreQueryFilters().AsQueryable();
            if (status.HasValue)
                query = query.Where(x => (int)x.Status == status.Value);
            if (consultantId.HasValue)
                query = query.Where(x => x.ConsultantUserId == consultantId.Value);
            if (studentId.HasValue)
                query = query.Where(x => x.StudentUserId == studentId.Value);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.ScheduledStartUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return (items, total);
        }

        public async Task<BookingAnalyticsDto> GetAnalyticsAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var query = _db.Bookings.IgnoreQueryFilters().AsQueryable();
            if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue)   query = query.Where(x => x.CreatedAt <= to.Value);

            var all = await query.Select(x => new { x.Status, x.TotalChargedGbp, x.PlatformCommissionGbp }).ToListAsync(ct);

            var completed  = all.Count(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.Completed);
            var cancelled  = all.Count(x => (int)x.Status >= 10 && (int)x.Status <= 16);
            var disputed   = all.Count(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.Disputed);
            var active     = all.Count(x => x.Status == global::Faaz.Services.Booking.Domain.BookingEnums.BookingStatus.InProgress);

            return new BookingAnalyticsDto(
                TotalBookings:    all.Count,
                CompletedBookings: completed,
                CancelledBookings: cancelled,
                DisputedBookings:  disputed,
                TotalRevenueGbp:   all.Sum(x => x.TotalChargedGbp),
                PlatformRevenueGbp: all.Sum(x => x.PlatformCommissionGbp),
                ActiveSessions:    active);
        }
    }
}
