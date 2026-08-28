using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MassTransit;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class UpdatePersonalInfoCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdatePersonalInfoDto PutModel { get; set; } = null!;
}

internal sealed class UpdatePersonalInfoCommandHandler : IRequestHandler<UpdatePersonalInfoCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdatePersonalInfoCommandHandler(IConsultantProfileServices profileServices, IPublishEndpoint publishEndpoint)
    {
        _profileServices = profileServices;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UpdatePersonalInfoCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        profile.FullLegalName       = command.PutModel.FullLegalName;
        profile.DisplayName         = command.PutModel.DisplayName;
        // Photo is owned exclusively by PUT /consultant-profiles/{id}/photo — never touch it here,
        // or every personal-info save silently wipes out whatever photo was just uploaded alongside it.
        profile.CurrentRole         = command.PutModel.CurrentRole;
        profile.Institution         = command.PutModel.Institution;
        profile.LinkedInUrl         = command.PutModel.LinkedInUrl;
        profile.YearsOfExperience   = command.PutModel.YearsOfExperience;

        var activated = await _profileServices.TryAutoActivateAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);
        await ConsultantActivationPublisher.PublishIfActivatedAsync(activated, profile, _publishEndpoint, ct);
    }
}
