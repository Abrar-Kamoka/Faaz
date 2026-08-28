using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Booking.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    // Aliased: this entity type name collides with the same-named DbSet property below, which would
    // otherwise shadow the type in nameof(...) member-access expressions.
    using BookingStatusHistoryEntity = global::Faaz.Services.Booking.Domain.Entities.BookingStatusHistory;

    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.ConfigureUtcDateTimeConvention();
            base.ConfigureConventions(configurationBuilder);
        }

        public DbSet<Booking>              Bookings             => Set<Booking>();
        public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
        public DbSet<Session>              Sessions             => Set<Session>();
        public DbSet<SessionParticipant>   SessionParticipants  => Set<SessionParticipant>();
        public DbSet<SessionEvent>         SessionEvents        => Set<SessionEvent>();
        public DbSet<Review>               Reviews              => Set<Review>();
        public DbSet<RefundAppeal>         RefundAppeals        => Set<RefundAppeal>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema("booking");
            builder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

            // SrNo is managed by application code (NewSerialNumberAsync → MAX+1), not by the database.
            foreach (var entity in builder.Model.GetEntityTypes()
                .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            {
                builder.Entity(entity.ClrType)
                       .Property(nameof(BaseEntity.SrNo))
                       .ValueGeneratedNever();
            }

            builder.Entity<Booking>().ApplyStandardColumnOrder(
                nameof(Booking.StudentUserId), nameof(Booking.ConsultantUserId), nameof(Booking.ConsultantProfileId),
                nameof(Booking.SessionTypeId), nameof(Booking.SessionTypeName), nameof(Booking.DurationMinutes), nameof(Booking.CallType),
                nameof(Booking.SessionPriceGbp), nameof(Booking.PlatformCommissionGbp), nameof(Booking.PromoDiscountGbp),
                nameof(Booking.PromoCodeId), nameof(Booking.TotalChargedGbp), nameof(Booking.ScheduledStartUtc), nameof(Booking.ScheduledEndUtc),
                nameof(Booking.StudentTimezone), nameof(Booking.SlotReservedUntilUtc), nameof(Booking.Status), nameof(Booking.AcceptedAt),
                nameof(Booking.ExpiresAt), nameof(Booking.CompletedAt), nameof(Booking.SettledAt), nameof(Booking.StripePaymentIntentId),
                nameof(Booking.SessionBrief), nameof(Booking.SessionNotes), nameof(Booking.CancellationReason), nameof(Booking.CancellationNotes),
                nameof(Booking.RefundPercentage), nameof(Booking.DisputeReason), nameof(Booking.DisputeResolution),
                nameof(Booking.DisputeResolutionNote), nameof(Booking.DisputeResolvedAt));

            builder.Entity<BookingStatusHistory>().ApplyStandardColumnOrder(
                nameof(BookingStatusHistoryEntity.BookingId), nameof(BookingStatusHistoryEntity.FromStatus), nameof(BookingStatusHistoryEntity.ToStatus),
                nameof(BookingStatusHistoryEntity.ChangedByUserId), nameof(BookingStatusHistoryEntity.ChangedAt), nameof(BookingStatusHistoryEntity.Notes));

            builder.Entity<RefundAppeal>().ApplyStandardColumnOrder(
                nameof(RefundAppeal.BookingId), nameof(RefundAppeal.StudentUserId), nameof(RefundAppeal.Reason),
                nameof(RefundAppeal.RequestedAmountGbp), nameof(RefundAppeal.Status), nameof(RefundAppeal.SubmittedAt),
                nameof(RefundAppeal.ReviewedByAdminId), nameof(RefundAppeal.ReviewedAt), nameof(RefundAppeal.AdminNotes));

            builder.Entity<Review>().ApplyStandardColumnOrder(
                nameof(Review.BookingId), nameof(Review.SessionId), nameof(Review.StudentUserId), nameof(Review.ConsultantProfileId),
                nameof(Review.Rating), nameof(Review.ReviewText), nameof(Review.IsPublic));

            builder.Entity<Session>().ApplyStandardColumnOrder(
                nameof(Session.BookingId), nameof(Session.LiveKitRoomName), nameof(Session.LiveKitRoomSid), nameof(Session.Status),
                nameof(Session.RoomCreatedAt), nameof(Session.ActualStartUtc), nameof(Session.ActualEndUtc), nameof(Session.ActualDurationSeconds),
                nameof(Session.CompletionPct), nameof(Session.CreateRoomJobId), nameof(Session.NoShowJobId), nameof(Session.ForceCloseJobId));

            builder.Entity<SessionEvent>().ApplyStandardColumnOrder(
                nameof(SessionEvent.BookingId), nameof(SessionEvent.SessionId), nameof(SessionEvent.LiveKitRoomSid),
                nameof(SessionEvent.LiveKitEventId), nameof(SessionEvent.EventType), nameof(SessionEvent.ParticipantIdentity),
                nameof(SessionEvent.Role), nameof(SessionEvent.OccurredAtUtc), nameof(SessionEvent.RawWebhookPayload));

            builder.Entity<SessionParticipant>().ApplyStandardColumnOrder(
                nameof(SessionParticipant.BookingId), nameof(SessionParticipant.SessionId), nameof(SessionParticipant.UserId),
                nameof(SessionParticipant.Role), nameof(SessionParticipant.FirstJoinedUtc), nameof(SessionParticipant.LastJoinWindowStartUtc),
                nameof(SessionParticipant.LastLeftUtc), nameof(SessionParticipant.TotalSecondsInRoom), nameof(SessionParticipant.DisconnectionCount),
                nameof(SessionParticipant.CompletedPreSessionCheck), nameof(SessionParticipant.PendingReconnectionJobId),
                nameof(SessionParticipant.FinalStatus));

            base.OnModelCreating(builder);
        }
    }
}
