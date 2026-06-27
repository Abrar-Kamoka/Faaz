using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Booking.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;

    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

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

            base.OnModelCreating(builder);
        }
    }
}
