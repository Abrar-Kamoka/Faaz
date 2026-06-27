namespace Faaz.Services.Payment.Domain;

public static class PaymentEnums
{
    public enum PaymentStatus
    {
        Pending       = 1,
        Authorised    = 2,
        Captured      = 3,
        Failed        = 4,
        Cancelled     = 5,
        Refunded      = 6,
        PartialRefund = 7
    }

    public enum RefundStatus
    {
        Pending    = 1,
        Processing = 2,
        Succeeded  = 3,
        Failed     = 4
    }

    public enum PayoutStatus
    {
        Pending    = 1,
        Processing = 2,
        Paid       = 3,
        Failed     = 4,
        OnHold     = 5
    }

    public enum PromoDiscountType
    {
        Percentage = 1,
        FixedAmount = 2
    }
}
