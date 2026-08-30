using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.Infrastructure.Services;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;

public class GetConsultantProfileQuery : IRequest<ConsultantProfileDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetConsultantProfileQueryHandler : IRequestHandler<GetConsultantProfileQuery, ConsultantProfileDto>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IBookingReviewClient _reviewClient;

    public GetConsultantProfileQueryHandler(IConsultantProfileServices profileServices, IBookingReviewClient reviewClient)
    {
        _profileServices = profileServices;
        _reviewClient = reviewClient;
    }

    public async Task<ConsultantProfileDto> Handle(GetConsultantProfileQuery query, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdWithCollectionsAsync(query.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", query.UserId);

        // Consultant doesn't own review data — best-effort live lookup against Booking;
        // a failed lookup just leaves the rating at its zero-review default.
        var reviewSummary = await _reviewClient.GetReviewSummaryAsync(profile.Id, ct);

        return new ConsultantProfileDto
        {
            Id = profile.Id,
            ProfileId = profile.Id,
            UserId = profile.UserId,
            ApplicationId = profile.ApplicationId,
            FullLegalName = profile.FullLegalName,
            DisplayName = profile.DisplayName,
            ProfessionalPhotoUrl = profile.ProfessionalPhotoUrl,
            CurrentRole = profile.CurrentRole,
            Institution = profile.Institution,
            LinkedInUrl = profile.LinkedInUrl,
            YearsOfExperience = profile.YearsOfExperience,
            StudyLevelsOffered      = profile.StudyLevelsOffered ?? [],
            SubjectIds              = profile.Subjects.Select(s => s.SubjectId).ToArray(),
            UniversityIds           = profile.Universities.Select(u => u.UniversityId).ToArray(),
            ServiceIds              = profile.Services.Select(s => s.ServiceId).ToArray(),
            WrittenBio = profile.WrittenBio,
            IntroVideoUrl = profile.IntroVideoUrl,
            CallPreference = profile.CallPreference.ToString(),
            MinBookingNoticeHours = profile.MinBookingNoticeHours,
            MaxAdvanceBookingDays = profile.MaxAdvanceBookingDays,
            TimeZoneId = profile.TimeZoneId,
            IsProfileComplete = profile.IsProfileComplete,
            IsActive = profile.IsActive,
            IsVerified          = profile.IsFeatured,
            AverageRating       = reviewSummary?.AverageRating ?? 0m,
            ReviewCount         = reviewSummary?.TotalCount ?? 0,
            IsAvailableThisWeek = profile.AvailabilitySlots.Any(s => !s.IsBlockedDate),
            SessionTypes = profile.SessionTypes.Select(s => new SessionTypeDto
            {
                Id = s.Id,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes,
                PriceGbp = s.PriceGbp,
                Description = s.Description,
                IsActive = s.IsActive,
                SortOrder = s.SortOrder
            }).ToList(),
            AvailabilitySlots = profile.AvailabilitySlots
                .Where(s => !s.IsBlockedDate)
                .Select(s => new WeeklySlotDto
                {
                    DayOfWeek = (int)s.DayOfWeek!.Value,
                    StartTime = s.StartTimeLocal!.Value,
                    EndTime   = s.EndTimeLocal!.Value
                }).ToList(),
            BlockedDates = profile.AvailabilitySlots
                .Where(s => s.IsBlockedDate)
                .Select(b => new BlockedDateDto
                {
                    Date   = b.Date!.Value,
                    Reason = b.Reason
                }).ToList()
        };
    }
}
