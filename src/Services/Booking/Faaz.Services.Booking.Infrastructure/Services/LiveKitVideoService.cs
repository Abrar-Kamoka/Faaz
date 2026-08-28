using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.Infrastructure.Services;

internal sealed class LiveKitVideoService : IVideoService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _serverUrl;
    private readonly string _webhookSecret;
    private readonly ILogger<LiveKitVideoService> _logger;

    public LiveKitVideoService(IConfiguration config, ILogger<LiveKitVideoService> logger)
    {
        _apiKey        = config["LiveKit:ApiKey"]        ?? throw new InvalidOperationException("LiveKit:ApiKey not configured");
        _apiSecret     = config["LiveKit:ApiSecret"]     ?? throw new InvalidOperationException("LiveKit:ApiSecret not configured");
        _serverUrl     = config["LiveKit:ServerUrl"]     ?? "http://localhost:7880";
        // LiveKit signs outgoing webhooks with the secret of one of its own configured API keys —
        // there's no separate "webhook secret" concept server-side (see config-sample.yaml upstream).
        // Only set LiveKit:WebhookSecret explicitly if the server is deliberately configured with a
        // second, dedicated key for webhook.api_key; otherwise this must equal ApiSecret, so default
        // to it here rather than requiring every environment to duplicate the value.
        _webhookSecret = config["LiveKit:WebhookSecret"] ?? _apiSecret;
        _logger        = logger;
    }

    public async Task<string> CreateRoomAsync(string roomName, int emptyTimeoutSeconds, CancellationToken ct = default)
    {
        var roomClient = new RoomServiceClient(_serverUrl, _apiKey, _apiSecret);
        var request    = new CreateRoomRequest
        {
            Name            = roomName,
            EmptyTimeout    = (uint)emptyTimeoutSeconds,
            MaxParticipants = 2
        };
        var room = await roomClient.CreateRoom(request);
        _logger.LogInformation("LiveKit room created: {RoomName} (SID: {Sid})", roomName, room.Sid);
        return room.Sid;
    }

    public Task<string> GenerateParticipantTokenAsync(
        string roomName, string identity, string displayName,
        bool canPublish, int ttlSeconds, CancellationToken ct = default)
    {
        var token = new AccessToken(_apiKey, _apiSecret)
            .WithIdentity(identity)
            .WithName(displayName)
            .WithTtl(TimeSpan.FromSeconds(ttlSeconds))
            .WithGrants(new VideoGrants
            {
                RoomJoin     = true,
                Room         = roomName,
                CanPublish   = canPublish,
                CanSubscribe = true
            });

        return Task.FromResult(token.ToJwt());
    }

    public async Task DeleteRoomAsync(string roomName, CancellationToken ct = default)
    {
        try
        {
            var roomClient = new RoomServiceClient(_serverUrl, _apiKey, _apiSecret);
            await roomClient.DeleteRoom(new DeleteRoomRequest { Room = roomName });
            _logger.LogInformation("LiveKit room deleted: {RoomName}", roomName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete LiveKit room {RoomName} — may already be closed", roomName);
        }
    }

    public bool VerifyWebhookSignature(string body, string authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return false;

        var token    = authHeader["Bearer ".Length..];
        var verifier = new WebhookReceiver(_apiKey, _webhookSecret);
        try
        {
            verifier.Receive(body, token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
