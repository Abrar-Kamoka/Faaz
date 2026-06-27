using System.Text.Json.Serialization;

namespace Faaz.Services.Booking.WebHost.Features.Sessions.Webhooks;

public class LiveKitWebhookPayload
{
    [JsonPropertyName("id")]          public string Id           { get; set; } = "";
    [JsonPropertyName("event")]       public string Event        { get; set; } = "";
    [JsonPropertyName("createdAt")]   public long   CreatedAt    { get; set; }
    [JsonPropertyName("room")]        public LiveKitRoomData?    Room        { get; set; }
    [JsonPropertyName("participant")] public LiveKitParticipantData? Participant { get; set; }
}

public class LiveKitRoomData
{
    [JsonPropertyName("name")]            public string Name            { get; set; } = "";
    [JsonPropertyName("sid")]             public string Sid             { get; set; } = "";
    [JsonPropertyName("numParticipants")] public int    NumParticipants { get; set; }
}

public class LiveKitParticipantData
{
    [JsonPropertyName("identity")] public string Identity { get; set; } = "";
    [JsonPropertyName("sid")]      public string Sid      { get; set; } = "";
}
