using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;

public class GetConsultantProfileQuery : IRequest<ConsultantProfileDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetConsultantProfileQueryHandler : IRequestHandler<GetConsultantProfileQuery, ConsultantProfileDto>
{
    private readonly IConsultantProfileServices _profileServices;

    public GetConsultantProfileQueryHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task<ConsultantProfileDto> Handle(GetConsultantProfileQuery query, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdWithCollectionsAsync(query.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", query.UserId);

        return new ConsultantProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            ApplicationId = profile.ApplicationId,
            FullLegalName = profile.FullLegalName,
            DisplayName = profile.DisplayName,
            ProfessionalPhotoUrl = profile.ProfessionalPhotoUrl,
            CurrentRole = profile.CurrentRole,
            Institution = profile.Institution,
            LinkedInUrl = profile.LinkedInUrl,
            YearsOfExperience = profile.YearsOfExperience,
            StudyLevelsOffered      = profile.StudyLevelsOffered.Select(x => (StudyLevel)x).ToArray(),
            SubjectAreas            = profile.SubjectAreas,
            SpecialisedUniversities = profile.SpecialisedUniversities,
            ServicesOffered         = profile.ServicesOffered.Select(x => (ServiceType)x).ToArray(),
            WrittenBio = profile.WrittenBio,
            IntroVideoUrl = profile.IntroVideoUrl,
            CallPreference = profile.CallPreference.ToString(),
            MinBookingNoticeHours = profile.MinBookingNoticeHours,
            MaxAdvanceBookingDays = profile.MaxAdvanceBookingDays,
            IsProfileComplete = profile.IsProfileComplete,
            IsActive = profile.IsActive,
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
                    StartTime = s.StartTimeUtc!.Value,
                    EndTime   = s.EndTimeUtc!.Value
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
