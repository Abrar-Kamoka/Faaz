using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class UpdateConsultantPhotoCommand : IRequest
{
    public required Guid UserId { get; init; }
    public required string PhotoUrl { get; init; }
}

internal sealed class UpdateConsultantPhotoCommandHandler : IRequestHandler<UpdateConsultantPhotoCommand>
{
    private readonly IConsultantProfileServices _profileServices;

    public UpdateConsultantPhotoCommandHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateConsultantPhotoCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        profile.ProfessionalPhotoUrl = command.PhotoUrl;
        await _profileServices.SaveChangesAsync(ct);
    }
}
