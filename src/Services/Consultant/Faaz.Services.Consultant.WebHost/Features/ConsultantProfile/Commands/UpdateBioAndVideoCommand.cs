using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MassTransit;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class UpdateBioAndVideoCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateBioAndVideoDto PutModel { get; set; } = null!;
}

internal sealed class UpdateBioAndVideoCommandHandler : IRequestHandler<UpdateBioAndVideoCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateBioAndVideoCommandHandler(IConsultantProfileServices profileServices, IPublishEndpoint publishEndpoint)
    {
        _profileServices = profileServices;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UpdateBioAndVideoCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        profile.WrittenBio = command.PutModel.WrittenBio;
        profile.IntroVideoUrl = command.PutModel.IntroVideoUrl;

        var activated = await _profileServices.TryAutoActivateAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);
        await ConsultantActivationPublisher.PublishIfActivatedAsync(activated, profile, _publishEndpoint, ct);
    }
}
