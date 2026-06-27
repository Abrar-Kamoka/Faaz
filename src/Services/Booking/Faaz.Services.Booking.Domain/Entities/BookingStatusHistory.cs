using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class BookingStatusHistory : BaseEntity
{
    public BookingStatusHistory()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid          BookingId       { get; set; }
    public BookingStatus FromStatus      { get; set; }
    public BookingStatus ToStatus        { get; set; }
    public Guid?         ChangedByUserId { get; set; }
    public DateTime      ChangedAt       { get; set; } = DateTime.UtcNow;
    public string?       Notes           { get; set; }
    public string?       Remarks         { get; set; }
    public string?       ExtraField1     { get; set; }
    public string?       ExtraField2     { get; set; }

    public Booking Booking { get; set; } = null!;
}

