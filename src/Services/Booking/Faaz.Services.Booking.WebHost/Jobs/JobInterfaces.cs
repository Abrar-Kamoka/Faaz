namespace Faaz.Services.Booking.WebHost.Jobs;

public interface ICreateSessionRoomJob
{
    Task ExecuteAsync(Guid bookingId);
}

public interface INoShowCheckJob
{
    Task ExecuteAsync(Guid bookingId);
}

public interface IForceCloseRoomJob
{
    Task ExecuteAsync(Guid bookingId);
}

public interface IReconnectionWindowExpiredJob
{
    Task ExecuteAsync(Guid bookingId);
}

public interface IExpireUnconfirmedBookingsJob
{
    Task ExecuteAsync();
}

public interface ICleanupExpiredSlotsJob
{
    Task ExecuteAsync();
}

public interface ISendSessionReminderJob
{
    Task ExecuteAsync(Guid bookingId, string reminderType);
}

public interface IReleasePendingPayoutsJob
{
    Task ExecuteAsync();
}
