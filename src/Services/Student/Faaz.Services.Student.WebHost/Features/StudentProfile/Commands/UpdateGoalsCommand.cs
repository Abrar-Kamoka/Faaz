using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
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

    public UpdateGoalsCommandHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateGoalsCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        profile.TargetStudyLevel = command.PutModel.TargetStudyLevel;
        profile.TargetSubjects = command.PutModel.TargetSubjects;
        profile.TargetUniversities = command.PutModel.TargetUniversities;
        profile.HelpTypes = command.PutModel.HelpTypes;

        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);
    }
}
