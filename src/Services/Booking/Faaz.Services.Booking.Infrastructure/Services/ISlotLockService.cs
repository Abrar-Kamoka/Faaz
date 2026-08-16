namespace Faaz.Services.Booking.Infrastructure.Services;

// Short-lived exclusive lock used to reserve a booking slot while payment is completed.
// InMemorySlotLockService (Development, no external dependency) and RedisSlotLockService
// (Staging/Production, safe across multiple service instances) are the two implementations.
public interface ISlotLockService
{
    // Atomically acquires the lock if not already held. Returns false if someone else holds it.
    Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task ReleaseAsync(string key, CancellationToken ct = default);
}
