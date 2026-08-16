using Faaz.SharedKernel.Entities;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.Domain.Entities;

public class PromoCode : BaseSoftDeleteModel
{
    public PromoCode()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string              Code              { get; set; } = string.Empty;
    public PromoDiscountType   DiscountType      { get; set; }
    public decimal             DiscountValue     { get; set; }
    public decimal?            MaxDiscountAmount { get; set; }
    public int?                MaxUses           { get; set; }
    public int                 CurrentUses       { get; set; } = 0;
    public DateTime?           ValidFrom         { get; set; }
    public DateTime?           ValidTo           { get; set; }
    public bool                IsActive          { get; set; } = true;
    public string?             Description       { get; set; }
    public Guid?               ConsultantProfileId { get; set; }
}
