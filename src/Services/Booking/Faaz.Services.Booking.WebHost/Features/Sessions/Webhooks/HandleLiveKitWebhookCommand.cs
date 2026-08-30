using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.Services.Booking.WebHost.Jobs;
using Faaz.SharedKernel.IntegrationEvents;
using Hangfire;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Faaz.Services.Booking.WebHost.Features.Sessions.Webhooks
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class HandleLiveKitWebhookCommand : IRequest
    {
        public string Body                { get; set; } = "";
        public string AuthorizationHeader { get; set; } = "";
    }

    public class HandleLiveKitWebhookCommandHandler : IRequestHandler<HandleLiveKitWebhookCommand>
    {
        private readonly IVideoService _videoService;
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly ISessionParticipantServices _participantServices;
        private readonly ISessionEventServices _eventServices;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IBackgroundJobClient _jobs;
        private readonly ILogger<HandleLiveKitWebhookCommandHandler> _logger;

        public HandleLiveKitWebhookCommandHandler(
            IVideoService videoService,
            IBookingServices bookingServices,
            ISessionServices sessionServices,
            ISessionParticipantServices participantServices,
            ISessionEventServices eventServices,
            IPublishEndpoint publishEndpoint,
            IBackgroundJobClient jobs,
            ILogger<HandleLiveKitWebhookCommandHandler> logger)
        {
            _videoService        = videoService;
            _bookingServices     = bookingServices;
            _sessionServices     = sessionServices;
            _participantServices = participantServices;
            _eventServices       = eventServices;
            _publishEndpoint     = publishEndpoint;
            _jobs                = jobs;
            _logger              = logger;
        }

        public async Task Handle(HandleLiveKitWebhookCommand command, CancellationToken ct)
        {
            if (!_videoService.VerifyWebhookSignature(command.Body, command.AuthorizationHeader))
            {
                _logger.LogWarning("LiveKit webhook signature verification failed");
                return;
            }

            LiveKitWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<LiveKitWebhookPayload>(command.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize LiveKit webhook payload");
                return;
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Id) || string.IsNullOrWhiteSpace(payload.Event))
                return;

            if (await _eventServices.ExistsByLiveKitEventIdAsync(payload.Id, ct))
            {
                _logger.LogDebug("LiveKit event {Id} already processed — skipping", payload.Id);
                return;
            }

            var roomName = payload.Room?.Name;
            if (string.IsNullOrWhiteSpace(roomName))
                return;

            var session = await _sessionServices.GetByRoomNameAsync(roomName, ct);
            if (session is null)
            {
                _logger.LogWarning("LiveKit webhook: no session found for room {Room}", roomName);
                return;
            }

            var booking = await _bookingServices.GetByIdWithDetailsAsync(session.BookingId, ct);
            if (booking is null) return;

            var occurredAt = DateTimeOffset.FromUnixTimeSeconds(payload.CreatedAt).UtcDateTime;

            switch (payload.Event)
            {
                case "room_started":
                    await HandleRoomStartedAsync(session, payload, occurredAt, ct);
                    break;
                case "participant_joined":
                    await HandleParticipantJoinedAsync(session, booking, payload, occurredAt, ct);
                    break;
                case "participant_left":
                    await HandleParticipantLeftAsync(session, booking, payload, occurredAt, ct);
                    break;
                case "room_finished":
                    await HandleRoomFinishedAsync(session, booking, payload, occurredAt, ct);
                    break;
                default:
                    _logger.LogDebug("LiveKit event type {Type} not handled", payload.Event);
                    await RecordEventAsync(session, payload, SessionEventType.RoomCreated, null, null, occurredAt, ct);
                    return;
            }
        }

        private async Task HandleRoomStartedAsync(Session session, LiveKitWebhookPayload payload, DateTime occurredAt, CancellationToken ct)
        {
            if (session.Status < SessionStatus.RoomReady)
            {
                session.Status         = SessionStatus.RoomReady;
                session.LiveKitRoomSid = payload.Room?.Sid;
                session.RoomCreatedAt  = occurredAt;
                await _sessionServices.SaveChangesAsync(ct);
            }

            await RecordEventAsync(session, payload, SessionEventType.RoomCreated, null, null, occurredAt, ct);
            _logger.LogInformation("LiveKit room started for session {SessionId}", session.Id);
        }

        private async Task HandleParticipantJoinedAsync(Session session, Booking booking, LiveKitWebhookPayload payload, DateTime occurredAt, CancellationToken ct)
        {
            var identity = payload.Participant?.Identity ?? "";
            var role     = ResolveRole(identity);
            var userId   = ResolveUserId(identity);

            if (userId != Guid.Empty)
            {
                var participant = await _participantServices.GetBySessionAndUserAsync(session.Id, userId, ct);
                if (participant is not null)
                {
                    if (!participant.FirstJoinedUtc.HasValue)
                        participant.FirstJoinedUtc = occurredAt;

                    // Track the start of this connection window so participant_left can accumulate duration
                    participant.LastJoinWindowStartUtc = occurredAt;

                    if (!string.IsNullOrWhiteSpace(participant.PendingReconnectionJobId))
                    {
                        _jobs.Delete(participant.PendingReconnectionJobId);
                        participant.PendingReconnectionJobId = null;
                    }

                    await _participantServices.SaveChangesAsync(ct);
                }
            }

            if (!string.IsNullOrWhiteSpace(session.NoShowJobId))
            {
                _jobs.Delete(session.NoShowJobId);
                session.NoShowJobId = null;
            }

            var participants = await _participantServices.GetBySessionIdAsync(session.Id, ct);
            var studentIn    = participants.Any(p => p.Role == ParticipantRole.Student);
            var consultantIn = participants.Any(p => p.Role == ParticipantRole.Consultant);

            if (studentIn && consultantIn && session.Status < SessionStatus.InProgress)
            {
                session.Status         = SessionStatus.InProgress;
                session.ActualStartUtc = session.ActualStartUtc ?? occurredAt;
                booking.Status         = BookingStatus.InProgress;
                await _bookingServices.SaveChangesAsync(ct);
            }
            else if (session.Status < SessionStatus.StudentWaiting)
            {
                session.Status = role == ParticipantRole.Student
                    ? SessionStatus.StudentWaiting
                    : SessionStatus.ConsultantWaiting;
            }

            await _sessionServices.SaveChangesAsync(ct);
            await RecordEventAsync(session, payload, SessionEventType.ParticipantJoined, identity, role, occurredAt, ct);
            _logger.LogInformation("Participant {Identity} joined session {SessionId}", identity, session.Id);
        }

        private async Task HandleParticipantLeftAsync(Session session, Booking booking, LiveKitWebhookPayload payload, DateTime occurredAt, CancellationToken ct)
        {
            var identity = payload.Participant?.Identity ?? "";
            var role     = ResolveRole(identity);
            var userId   = ResolveUserId(identity);

            if (userId != Guid.Empty)
            {
                var participant = await _participantServices.GetBySessionAndUserAsync(session.Id, userId, ct);
                if (participant is not null)
                {
                    // Accumulate the time spent in this connection window
                    if (participant.LastJoinWindowStartUtc.HasValue)
                    {
                        var secondsThisWindow = (int)(occurredAt - participant.LastJoinWindowStartUtc.Value).TotalSeconds;
                        if (secondsThisWindow > 0)
                            participant.TotalSecondsInRoom += secondsThisWindow;
                    }
                    participant.LastJoinWindowStartUtc = null; // connection window closed

                    participant.LastLeftUtc = occurredAt;
                    participant.DisconnectionCount++;

                    if (session.Status == SessionStatus.InProgress)
                    {
                        var jobId = _jobs.Schedule<IReconnectionWindowExpiredJob>(
                            j => j.ExecuteAsync(booking.Id),
                            TimeSpan.FromSeconds(90));
                        participant.PendingReconnectionJobId = jobId;
                        session.Status                       = SessionStatus.Interrupted;
                    }

                    await _participantServices.SaveChangesAsync(ct);
                }
            }

            await _sessionServices.SaveChangesAsync(ct);
            await RecordEventAsync(session, payload, SessionEventType.ParticipantLeft, identity, role, occurredAt, ct);
            _logger.LogInformation("Participant {Identity} left session {SessionId}", identity, session.Id);
        }

        private async Task HandleRoomFinishedAsync(Session session, Booking booking, LiveKitWebhookPayload payload, DateTime occurredAt, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(session.ForceCloseJobId))
            {
                _jobs.Delete(session.ForceCloseJobId);
                session.ForceCloseJobId = null;
            }

            session.ActualEndUtc = occurredAt;

            var actualSeconds = session.ActualStartUtc.HasValue
                ? (int)(occurredAt - session.ActualStartUtc.Value).TotalSeconds
                : 0;

            session.ActualDurationSeconds = actualSeconds;

            var participants = await _participantServices.GetBySessionIdAsync(session.Id, ct);

            // Flush any open connection windows for participants still in the room when it closed
            // (participant_left was never fired for them, so their last window is still open)
            foreach (var p in participants.Where(p => p.LastJoinWindowStartUtc.HasValue))
            {
                var secondsRemaining = (int)(occurredAt - p.LastJoinWindowStartUtc!.Value).TotalSeconds;
                if (secondsRemaining > 0)
                    p.TotalSecondsInRoom += secondsRemaining;
                p.LastJoinWindowStartUtc = null;
            }

            var studentSeconds    = participants.Where(p => p.Role == ParticipantRole.Student).Sum(p => p.TotalSecondsInRoom);
            var consultantSeconds = participants.Where(p => p.Role == ParticipantRole.Consultant).Sum(p => p.TotalSecondsInRoom);
            var bookedSeconds     = booking.DurationMinutes * 60;

            var completionPct = bookedSeconds > 0
                ? Math.Min(studentSeconds, consultantSeconds) / (decimal)bookedSeconds * 100m
                : 100m;

            session.CompletionPct = Math.Round(completionPct, 2);

            var isEarly     = completionPct < 80m;
            session.Status  = isEarly ? SessionStatus.CompletedEarly : SessionStatus.Completed;
            booking.Status  = isEarly ? BookingStatus.CompletedEarly : BookingStatus.Completed;
            booking.CompletedAt = occurredAt;

            foreach (var p in participants)
                p.FinalStatus = p.DisconnectionCount == 0
                    ? ParticipantConnectionStatus.JoinedCompleted
                    : ParticipantConnectionStatus.DisconnectedAndRejoined;

            // Published before the SaveChangesAsync calls below so the EF outbox captures it atomically.
            await _publishEndpoint.Publish(new SessionCompletedEvent(booking.Id, new DateTimeOffset(occurredAt), actualSeconds), ct);

            await _sessionServices.SaveChangesAsync(ct);
            await _bookingServices.SaveChangesAsync(ct);

            await RecordEventAsync(session, payload,
                isEarly ? SessionEventType.SessionCompletedEarly : SessionEventType.SessionCompleted,
                null, null, occurredAt, ct);

            _logger.LogInformation("LiveKit room finished for session {SessionId}; completionPct={Pct}%", session.Id, session.CompletionPct);
        }

        private async Task RecordEventAsync(
            Session session, LiveKitWebhookPayload payload,
            SessionEventType eventType, string? identity, ParticipantRole? role,
            DateTime occurredAt, CancellationToken ct)
        {
            var evt = new SessionEvent
            {
                BookingId           = session.BookingId,
                SessionId           = session.Id,
                LiveKitRoomSid      = payload.Room?.Sid ?? session.LiveKitRoomSid ?? "",
                LiveKitEventId      = payload.Id,
                EventType           = eventType,
                ParticipantIdentity = identity,
                Role                = role,
                OccurredAtUtc       = occurredAt,
                RawWebhookPayload   = payload.Id
            };
            await _eventServices.AddAsync(evt, ct);
            await _eventServices.SaveChangesAsync(ct);
        }

        private static ParticipantRole ResolveRole(string identity)
            => identity.StartsWith("student-") ? ParticipantRole.Student : ParticipantRole.Consultant;

        private static Guid ResolveUserId(string identity)
        {
            var dashIdx = identity.IndexOf('-');
            if (dashIdx < 0) return Guid.Empty;
            var idPart = identity[(dashIdx + 1)..];
            return Guid.TryParseExact(idPart, "N", out var id) ? id : Guid.Empty;
        }
    }
}
