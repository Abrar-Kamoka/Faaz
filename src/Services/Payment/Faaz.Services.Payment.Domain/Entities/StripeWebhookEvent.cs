using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Payment.Domain.Entities;

public class StripeWebhookEvent : BaseEntity
{
    public StripeWebhookEvent()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string    StripeEventId { get; set; } = string.Empty;
    public string    EventType     { get; set; } = string.Empty;
    public string    PayloadJson   { get; set; } = string.Empty;
    public bool      Processed     { get; set; } = false;
    public DateTime? ProcessedAt   { get; set; }
    public string?   ErrorMessage  { get; set; }
    public DateTime  ReceivedAt    { get; set; } = DateTime.UtcNow;
}
