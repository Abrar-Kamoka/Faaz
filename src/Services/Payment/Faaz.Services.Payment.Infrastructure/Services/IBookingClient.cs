namespace Faaz.Services.Payment.Infrastructure.Services;

public interface IBookingClient
{
    Task<BookingPaymentDetailsResult?> GetBookingDetailsAsync(Guid bookingId, CancellationToken ct = default);
}

public record BookingPaymentDetailsResult(Guid BookingId, Guid StudentUserId, Guid ConsultantUserId, decimal TotalChargedGbp);
