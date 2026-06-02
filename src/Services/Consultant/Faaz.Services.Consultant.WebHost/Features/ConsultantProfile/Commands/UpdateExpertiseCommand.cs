using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
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

    public UpdateExpertiseCommandHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateExpertiseCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        profile.StudyLevelsOffered       = command.PutModel.StudyLevelsOffered.Select(x => (int)x).ToArray();
        profile.SubjectAreas             = command.PutModel.SubjectAreas;
        profile.SpecialisedUniversities  = command.PutModel.SpecialisedUniversities;
        profile.ServicesOffered          = command.PutModel.ServicesOffered.Select(x => (int)x).ToArray();

        await _profileServices.TryAutoActivateAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);
    }
}
