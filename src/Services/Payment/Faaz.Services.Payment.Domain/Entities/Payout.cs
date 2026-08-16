using Faaz.SharedKernel.Entities;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.Domain.Entities;

public class Payout : BaseSoftDeleteModel
{
    public Payout()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid          BookingId                 { get; set; }
    public Guid          ConsultantUserId          { get; set; }
    public string?       StripeConnectAccountId    { get; set; }
    public string?       StripeTransferId          { get; set; }
    public decimal       Amount                    { get; set; }
    public string        Currency                  { get; set; } = "gbp";
    public PayoutStatus  Status                    { get; set; } = PayoutStatus.Pending;
    public DateTime?     ScheduledReleaseAt        { get; set; }
    public DateTime?     ReleasedAt                { get; set; }
    public string?       FailureReason             { get; set; }
}
