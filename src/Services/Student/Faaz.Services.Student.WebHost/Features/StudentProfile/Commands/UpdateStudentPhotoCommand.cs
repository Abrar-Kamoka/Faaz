using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdateStudentPhotoCommand : IRequest
{
    public required Guid UserId { get; init; }
    public required string PhotoUrl { get; init; }
}

internal sealed class UpdateStudentPhotoCommandHandler : IRequestHandler<UpdateStudentPhotoCommand>
{
    private readonly IStudentProfileServices _profileServices;

    public UpdateStudentPhotoCommandHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateStudentPhotoCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        profile.ProfilePhotoUrl = command.PhotoUrl;
        await _profileServices.SaveChangesAsync(ct);
    }
}
