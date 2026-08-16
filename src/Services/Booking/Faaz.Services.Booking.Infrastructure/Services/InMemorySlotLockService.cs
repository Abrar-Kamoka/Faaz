using Microsoft.Extensions.Caching.Memory;

namespace Faaz.Services.Booking.Infrastructure.Services;

// Zero-setup slot lock for local dev / single-instance deployments — no external server required.
// Not safe across multiple process instances; use RedisSlotLockService for that.
internal sealed class InMemorySlotLockService : ISlotLockService
{
    private readonly IMemoryCache _cache;
    private static readonly object Gate = new();

    public InMemorySlotLockService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        lock (Gate)
        {
            if (_cache.TryGetValue(key, out _))
                return Task.FromResult(false);

            _cache.Set(key, true, ttl);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(key, out _));

    public Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
