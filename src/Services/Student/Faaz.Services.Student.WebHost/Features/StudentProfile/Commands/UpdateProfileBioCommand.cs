using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
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
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateProfileBioCommandHandler(IStudentProfileServices profileServices, IPublishEndpoint publishEndpoint)
    {
        _profileServices = profileServices;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UpdateProfileBioCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        var wasComplete = profile.IsOnboardingComplete;

        profile.Bio = command.PutModel.Bio;
        // Photo is owned exclusively by PUT /students/{id}/photo — never touch it here, or every
        // bio save silently wipes out whatever photo the student just uploaded in the same form.
        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);

        // Fire the one-time welcome notification exactly on the false→true transition — not on every
        // subsequent bio edit once onboarding is already complete.
        if (!wasComplete && profile.IsOnboardingComplete)
            await _publishEndpoint.Publish(new StudentOnboardingCompletedEvent(profile.UserId, profile.FirstName), ct);
    }
}
