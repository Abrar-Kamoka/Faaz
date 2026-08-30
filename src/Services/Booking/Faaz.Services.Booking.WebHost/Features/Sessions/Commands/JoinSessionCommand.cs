using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.Services.Booking.WebHost.Features.Sessions.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Faaz.Services.Booking.WebHost.Features.Sessions.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class JoinSessionCommand : IRequest<JoinSessionResultDto>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingUserId { get; set; }
        public string RequestingRole { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public JoinSessionDto PostModel { get; set; } = null!;
    }

    public class JoinSessionCommandHandler : IRequestHandler<JoinSessionCommand, JoinSessionResultDto>
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly ISessionParticipantServices _participantServices;
        private readonly IVideoService _videoService;
        private readonly IConfiguration _config;
        private readonly IBookingIdentityClient _identityClient;

        public JoinSessionCommandHandler(
            IBookingServices b, ISessionServices s, ISessionParticipantServices p, IVideoService v,
            IConfiguration c, IBookingIdentityClient identityClient)
        { _bookingServices = b; _sessionServices = s; _participantServices = p; _videoService = v; _config = c; _identityClient = identityClient; }

        public async Task<JoinSessionResultDto> Handle(JoinSessionCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            var isStudent    = command.RequestingRole == "1" && booking.StudentUserId == command.RequestingUserId;
            var isConsultant = command.RequestingRole == "2" && booking.ConsultantUserId == command.RequestingUserId;

            if (!isStudent && !isConsultant)
                throw new ForbiddenException("You are not a participant in this booking.");

            var joinableStatuses = new[] { BookingStatus.Confirmed, BookingStatus.InProgress };
            if (!joinableStatuses.Contains(booking.Status))
                throw BusinessRuleException.Error("This booking is not currently joinable.", "session.not-joinable");

            var now         = DateTime.UtcNow;
            var windowStart = booking.ScheduledStartUtc.AddMinutes(-5);
            var windowEnd   = booking.ScheduledEndUtc.AddMinutes(30);

            if (now < windowStart)
                throw BusinessRuleException.Error("The session room is not yet open. Please join within 5 minutes of the start time.", "session.not-open-yet");

            if (now > windowEnd)
                throw BusinessRuleException.Error("The session window has expired.", "session.expired");

            var session = booking.Session;
            if (session is null || string.IsNullOrEmpty(session.LiveKitRoomName))
                throw BusinessRuleException.Error("The session room is not yet ready. Please try again shortly.", "session.room-not-ready");

            var existing = await _participantServices.GetBySessionAndUserAsync(session.Id, command.RequestingUserId, ct);
            if (existing is null)
            {
                var srNo = await _participantServices.NewSerialNumberAsync(ct);
                await _participantServices.AddAsync(new SessionParticipant
                {
                    SrNo                     = srNo,
                    BookingId                = booking.Id,
                    SessionId                = session.Id,
                    UserId                   = command.RequestingUserId,
                    Role                     = isStudent ? ParticipantRole.Student : ParticipantRole.Consultant,
                    CompletedPreSessionCheck = command.PostModel.PreSessionCheckCompleted
                }, ct);
                await _participantServices.SaveChangesAsync(ct);
            }
            else if (command.PostModel.PreSessionCheckCompleted && !existing.CompletedPreSessionCheck)
            {
                existing.CompletedPreSessionCheck = true;
                await _participantServices.SaveChangesAsync(ct);
            }

            var identity   = isStudent ? $"student-{command.RequestingUserId:N}" : $"consultant-{command.RequestingUserId:N}";
            var ttlSeconds = (int)(windowEnd - now).TotalSeconds + 300;

            // The JWT carries no display name (sub/userId/email/role only — see TokenService), so
            // the caller-supplied DisplayName is never anything but a "Participant" fallback string.
            // Look up the real name from Identity instead; keep the fallback only if that lookup fails.
            var nameResult = await _identityClient.GetUserNameAsync(command.RequestingUserId, ct);
            var displayName = nameResult?.FullName ?? command.DisplayName;

            var token      = await _videoService.GenerateParticipantTokenAsync(
                session.LiveKitRoomName, identity, displayName, canPublish: true, ttlSeconds, ct);

            return new JoinSessionResultDto
            {
                RoomName               = session.LiveKitRoomName,
                Token                  = token,
                ServerUrl              = _config["LiveKit:ServerUrl"] ?? "http://localhost:7880",
                PreSessionCheckRequired = !command.PostModel.PreSessionCheckCompleted
            };
        }
    }
}
