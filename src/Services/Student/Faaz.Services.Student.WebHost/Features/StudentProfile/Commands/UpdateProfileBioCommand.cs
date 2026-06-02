using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdateProfileBioCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateProfileBioDto PutModel { get; set; } = null!;
}

internal sealed class UpdateProfileBioCommandHandler : IRequestHandler<UpdateProfileBioCommand>
{
    private readonly IStudentProfileServices _profileServices;

    public UpdateProfileBioCommandHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateProfileBioCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        profile.Bio = command.PutModel.Bio;
        profile.ProfilePhotoUrl = command.PutModel.ProfilePhotoUrl;

        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);
    }
}
