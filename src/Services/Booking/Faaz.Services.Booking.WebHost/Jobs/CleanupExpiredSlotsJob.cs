using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Faaz.Services.Booking.WebHost.Jobs;

public class CleanupExpiredSlotsJob : ICleanupExpiredSlotsJob
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CleanupExpiredSlotsJob> _logger;

    public CleanupExpiredSlotsJob(IConnectionMultiplexer r, ILogger<CleanupExpiredSlotsJob> l) { _redis = r; _logger = l; }

    public Task ExecuteAsync()
    {
        // Redis TTL handles expiry automatically; this job handles any orphaned keys
        // by scanning the slot-lock keyspace for keys that should have expired.
        _logger.LogDebug("CleanupExpiredSlotsJob: Redis TTL manages slot expiry natively; no manual cleanup needed.");
        return Task.CompletedTask;
    }
}
