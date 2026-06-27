using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class RefundAppeal : BaseEntity
{
    public RefundAppeal()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid               BookingId          { get; set; }
    public Guid               StudentUserId      { get; set; }
    public string             Reason             { get; set; } = string.Empty;
    public RefundAppealStatus Status             { get; set; } = RefundAppealStatus.Pending;
    public decimal            RequestedAmountGbp { get; set; }
    public DateTime           SubmittedAt        { get; set; } = DateTime.UtcNow;
    public Guid?              ReviewedByAdminId  { get; set; }
    public DateTime?          ReviewedAt         { get; set; }
    public string?            AdminNotes         { get; set; }
    public string?            Remarks            { get; set; }
    public string?            ExtraField1        { get; set; }
    public string?            ExtraField2        { get; set; }

    public Booking Booking { get; set; } = null!;
}

