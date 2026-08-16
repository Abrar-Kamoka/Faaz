using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class Review : BaseSoftDeleteModel
{
    public Review()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid         BookingId           { get; set; }
    public Guid         SessionId           { get; set; }
    public Guid         StudentUserId       { get; set; }
    public Guid         ConsultantProfileId { get; set; }
    public ReviewRating Rating              { get; set; }
    public string?      ReviewText          { get; set; }
    public bool         IsPublic            { get; set; } = true;
    public new DateTime CreatedAt           { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public Session Session { get; set; } = null!;
}

