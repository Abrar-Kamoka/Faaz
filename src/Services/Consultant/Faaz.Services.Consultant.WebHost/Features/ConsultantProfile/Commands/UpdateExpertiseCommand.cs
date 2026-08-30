using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.HttpClients;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MassTransit;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class UpdateExpertiseCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateExpertiseDto PutModel { get; set; } = null!;
}

internal sealed class UpdateExpertiseCommandHandler : IRequestHandler<UpdateExpertiseCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IAdministrationReferenceClient _referenceClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateExpertiseCommandHandler(
        IConsultantProfileServices profileServices,
        IAdministrationReferenceClient referenceClient,
        IPublishEndpoint publishEndpoint)
    {
        _profileServices = profileServices;
        _referenceClient = referenceClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UpdateExpertiseCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdWithCollectionsAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        var model = command.PutModel;

        // This is what stops a consultant claiming affiliation with a university/subject/service
        // that doesn't exist in the real, admin-curated catalog — checked before anything is saved.
        var validation = await _referenceClient.ValidateAsync(model.UniversityIds, null, model.SubjectIds, model.ServiceIds, ct);
        if (validation is null)
            throw BusinessRuleException.Error("Could not verify the selected universities, subjects, and services right now. Please try again.", "reference_validation_unavailable");
        if (validation.InvalidUniversityIds.Length > 0 || validation.InvalidSubjectIds.Length > 0 || validation.InvalidServiceIds.Length > 0)
            throw BusinessRuleException.Error(
                "One or more selected universities, subjects, or services are not recognized.",
                "reference_validation_failed",
                new Dictionary<string, string>
                {
                    ["invalidUniversityIds"] = string.Join(",", validation.InvalidUniversityIds),
                    ["invalidSubjectIds"]    = string.Join(",", validation.InvalidSubjectIds),
                    ["invalidServiceIds"]    = string.Join(",", validation.InvalidServiceIds)
                });

        profile.StudyLevelsOffered = model.StudyLevelsOffered;

        ReconcileServices(profile, model.ServiceIds);
        ReconcileSubjects(profile, model.SubjectIds);
        ReconcileUniversities(profile, model.UniversityIds);

        var activated = await _profileServices.TryAutoActivateAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);
        await ConsultantActivationPublisher.PublishIfActivatedAsync(activated, profile, _publishEndpoint, ct);
    }

    private static void ReconcileServices(Faaz.Services.Consultant.Domain.Entities.ConsultantProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.Services.Where(x => !wanted.Contains(x.ServiceId)).ToList())
            profile.Services.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.Services.All(x => x.ServiceId != id)))
            profile.Services.Add(new ConsultantProfileService { ConsultantProfileId = profile.Id, ServiceId = toAdd });
    }

    private static void ReconcileSubjects(Faaz.Services.Consultant.Domain.Entities.ConsultantProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.Subjects.Where(x => !wanted.Contains(x.SubjectId)).ToList())
            profile.Subjects.Remove(toRemove);
        foreach (var toAdd in wanted.Where(id => profile.Subjects.All(x => x.SubjectId != id)))
            profile.Subjects.Add(new ConsultantProfileSubject { ConsultantProfileId = profile.Id, SubjectId = toAdd });
    }

    private static void ReconcileUniversities(Faaz.Services.Consultant.Domain.Entities.ConsultantProfile profile, Guid[] wantedIds)
    {
        var wanted = wantedIds.ToHashSet();
        foreach (var toRemove in profile.Universities.Where(x => !wanted.Contains(x.UniversityId)).ToList())
            profile.Universities.Remove(toRemove);
        // A brand-new link starts unverified — IsVerified is only ever set true by a future admin
        // claim-verification workflow, never by the consultant's own submission.
        foreach (var toAdd in wanted.Where(id => profile.Universities.All(x => x.UniversityId != id)))
            profile.Universities.Add(new ConsultantProfileUniversity { ConsultantProfileId = profile.Id, UniversityId = toAdd });
    }
}
