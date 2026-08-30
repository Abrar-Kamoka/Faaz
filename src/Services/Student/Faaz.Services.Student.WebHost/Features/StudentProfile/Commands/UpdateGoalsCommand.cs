using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.Services.Student.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdateGoalsCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateGoalsDto PutModel { get; set; } = null!;
}

internal sealed class UpdateGoalsCommandHandler : IRequestHandler<UpdateGoalsCommand>
{
    private readonly IStudentProfileServices _profileServices;
    private readonly IAdministrationReferenceClient _referenceClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateGoalsCommandHandler(
        IStudentProfileServices profileServices,
        IAdministrationReferenceClient referenceClient,
        IPublishEndpoint publishEndpoint)
    {
        _profileServices = profileServices;
        _referenceClient = referenceClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UpdateGoalsCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        var model = command.PutModel;

        // This is what stops a student targeting a university/programme/subject/service that
        // doesn't exist in the real, admin-curated catalog — checked before anything is saved.
        var validation = await _referenceClient.ValidateAsync(model.TargetUniversityIds, model.TargetProgrammeIds, model.TargetSubjectIds, model.HelpServiceIds, ct);
        if (validation is null)
            throw BusinessRuleException.Error("Could not verify the selected universities, programmes, subjects, and services right now. Please try again.", "reference_validation_unavailable");
        if (validation.InvalidUniversityIds.Length > 0 || validation.InvalidProgrammeIds.Length > 0 || validation.InvalidSubjectIds.Length > 0 || validation.InvalidServiceIds.Length > 0)
            throw BusinessRuleException.Error(
                "One or more selected universities, programmes, subjects, or services are not recognized.",
                "reference_validation_failed",
                new Dictionary<string, string>
                {
                    ["invalidUniversityIds"] = string.Join(",", validation.InvalidUniversityIds),
                    ["invalidProgrammeIds"]  = string.Join(",", validation.InvalidProgrammeIds),
                    ["invalidSubjectIds"]    = string.Join(",", validation.InvalidSubjectIds),
                    ["invalidServiceIds"]    = string.Join(",", validation.InvalidServiceIds)
                });

        var wasComplete = profile.IsOnboardingComplete;

        profile.TargetStudyLevel = model.TargetStudyLevel;

        ReconcileHelpServices(profile, model.HelpServiceIds);
        ReconcileTargetUniversities(profile, model.TargetUniversityIds);
        ReconcileTargetSubjects(profile, model.TargetSubjectIds);
        ReconcileTargetProgrammes(profile, model.TargetProgrammeIds);

        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);

        if (!wasComplete && profile.IsOnboardingComplete)
            await _publishEndpoint.Publish(new StudentOnboardingCompletedEvent(profile.UserId, profile.FirstName), ct);
    }

    private static void ReconcileHelpServices(Faaz.Services.Student.Domain.Entities.StudentProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.HelpServices.Where(x => !wanted.Contains(x.ServiceId)).ToList())
            profile.HelpServices.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.HelpServices.All(x => x.ServiceId != id)))
            profile.HelpServices.Add(new StudentProfileHelpService { StudentProfileId = profile.Id, ServiceId = toAdd });
    }

    private static void ReconcileTargetUniversities(Faaz.Services.Student.Domain.Entities.StudentProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.TargetUniversities.Where(x => !wanted.Contains(x.UniversityId)).ToList())
            profile.TargetUniversities.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.TargetUniversities.All(x => x.UniversityId != id)))
            profile.TargetUniversities.Add(new StudentProfileTargetUniversity { StudentProfileId = profile.Id, UniversityId = toAdd });
    }

    private static void ReconcileTargetSubjects(Faaz.Services.Student.Domain.Entities.StudentProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.TargetSubjects.Where(x => !wanted.Contains(x.SubjectId)).ToList())
            profile.TargetSubjects.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.TargetSubjects.All(x => x.SubjectId != id)))
            profile.TargetSubjects.Add(new StudentProfileTargetSubject { StudentProfileId = profile.Id, SubjectId = toAdd });
    }

    private static void ReconcileTargetProgrammes(Faaz.Services.Student.Domain.Entities.StudentProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.TargetProgrammes.Where(x => !wanted.Contains(x.ProgrammeId)).ToList())
            profile.TargetProgrammes.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.TargetProgrammes.All(x => x.ProgrammeId != id)))
            profile.TargetProgrammes.Add(new StudentProfileTargetProgramme { StudentProfileId = profile.Id, ProgrammeId = toAdd });
    }
}
