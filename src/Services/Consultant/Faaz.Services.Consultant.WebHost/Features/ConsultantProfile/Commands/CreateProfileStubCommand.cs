using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class CreateProfileStubCommand : IRequest
{
    public required Guid UserId { get; init; }
    /// <summary>
    /// Fallback when the application's UserId link was never set (e.g. Consultant service was
    /// down during account creation). Allows lookup by email so the stub can still be created.
    /// </summary>
    public string? Email { get; init; }
}

internal sealed class CreateProfileStubCommandHandler : IRequestHandler<CreateProfileStubCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IConsultantApplicationServices _applicationServices;
    private readonly ILogger<CreateProfileStubCommandHandler> _logger;

    public CreateProfileStubCommandHandler(
        IConsultantProfileServices profileServices,
        IConsultantApplicationServices applicationServices,
        ILogger<CreateProfileStubCommandHandler> logger)
    {
        _profileServices = profileServices;
        _applicationServices = applicationServices;
        _logger = logger;
    }

    public async Task Handle(CreateProfileStubCommand command, CancellationToken ct)
    {
        if (await _profileServices.ExistsForUserAsync(command.UserId, ct))
            return;

        // Try by UserId first (fast path). Fall back to email if the link was never persisted.
        var application = await _applicationServices.GetByUserIdAsync(command.UserId, ct);

        if (application is null && !string.IsNullOrWhiteSpace(command.Email))
        {
            application = await _applicationServices.GetByEmailAsync(command.Email, ct);
            if (application is not null)
            {
                // Repair the missing link so future lookups succeed.
                application.UserId = command.UserId;
                await _applicationServices.SaveChangesAsync(ct);
                _logger.LogWarning(
                    "Repaired missing UserId link on ConsultantApplication {ApplicationId} for user {UserId}",
                    application.Id, command.UserId);
            }
        }

        if (application is null)
            throw new NotFoundException("ConsultantApplication", command.UserId);

        var (studyLevels, subjects, universities, services) = ParseExpertiseArea(application.ExpertiseArea);

        // Pre-fill from the EoI so the consultant doesn't re-enter data they already provided.
        var profile = new Domain.Entities.ConsultantProfile
        {
            UserId                  = command.UserId,
            ApplicationId           = application.Id,
            FullLegalName           = $"{application.FirstName} {application.LastName}".Trim(),
            DisplayName             = application.FirstName,
            CurrentRole             = application.CurrentRole,
            Institution             = application.Institution ?? string.Empty,
            LinkedInUrl             = application.LinkedInProfileUrl,
            YearsOfExperience       = application.YearsOfExperience,
            StudyLevelsOffered      = studyLevels,
            SubjectAreas            = subjects,
            SpecialisedUniversities = universities,
            ServicesOffered         = services
        };

        await _profileServices.AddAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);

        _logger.LogInformation("Consultant profile stub created. UserId: {UserId}", command.UserId);
    }

    // The join-as-consultant EoI wizard has no structured columns for the study-levels/subjects/
    // universities/services the applicant picks — it flattens all four into one reporting string
    // on submit, e.g. "Levels: Undergraduate | Subjects: Physics, Maths | Universities: Oxford |
    // Services: UCAS Guidance". Unpack that same format here so the setup wizard's Expertise step
    // (which already pre-fills from these exact profile fields) shows it without asking again.
    private static readonly Dictionary<string, StudyLevel> StudyLevelLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sixth Form / A-Levels"]    = StudyLevel.ALevel,
        ["Undergraduate"]            = StudyLevel.Undergraduate,
        ["Postgraduate (MSc/PhD)"]   = StudyLevel.Postgraduate,
    };

    private static readonly Dictionary<string, ServiceType> ServiceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Personal Statement Review"] = ServiceType.PersonalStatement,
        ["UCAS Guidance"]             = ServiceType.Ucas,
        ["Interview Preparation"]     = ServiceType.InterviewPrep,
        ["SOP Review"]                = ServiceType.Sop,
        ["Scholarships"]              = ServiceType.Scholarships,
        ["Visa Guidance"]             = ServiceType.Visa,
        ["General Guidance"]          = ServiceType.GeneralGuidance,
    };

    private static (int[] StudyLevels, string[] Subjects, string[] Universities, int[] Services) ParseExpertiseArea(string? expertiseArea)
    {
        if (string.IsNullOrWhiteSpace(expertiseArea))
            return ([], [], [], []);

        var studyLevels  = new List<int>();
        var subjects     = new List<string>();
        var universities = new List<string>();
        var services     = new List<int>();

        foreach (var segment in expertiseArea.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            var values = parts[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "Levels":
                    studyLevels.AddRange(values.Where(StudyLevelLabels.ContainsKey).Select(v => (int)StudyLevelLabels[v]));
                    break;
                case "Subjects":
                    subjects.AddRange(values);
                    break;
                case "Universities":
                    universities.AddRange(values);
                    break;
                case "Services":
                    services.AddRange(values.Where(ServiceLabels.ContainsKey).Select(v => (int)ServiceLabels[v]));
                    break;
            }
        }

        return (studyLevels.ToArray(), subjects.ToArray(), universities.ToArray(), services.ToArray());
    }
}
