namespace Faaz.Services.Booking.Infrastructure.Services;

public interface IVideoService
{
    Task<string> CreateRoomAsync(string roomName, int emptyTimeoutSeconds, CancellationToken ct = default);
    Task<string> GenerateParticipantTokenAsync(string roomName, string identity, string displayName, bool canPublish, int ttlSeconds, CancellationToken ct = default);
    Task DeleteRoomAsync(string roomName, CancellationToken ct = default);
    bool VerifyWebhookSignature(string body, string authHeader);
}
