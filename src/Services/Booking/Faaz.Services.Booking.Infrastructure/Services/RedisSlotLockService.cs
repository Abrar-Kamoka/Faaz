using StackExchange.Redis;

namespace Faaz.Services.Booking.Infrastructure.Services;

// Redis-backed slot lock — safe across multiple service instances. Used in Staging/Production.
internal sealed class RedisSlotLockService : ISlotLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSlotLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(key, "1", ttl, When.NotExists);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
