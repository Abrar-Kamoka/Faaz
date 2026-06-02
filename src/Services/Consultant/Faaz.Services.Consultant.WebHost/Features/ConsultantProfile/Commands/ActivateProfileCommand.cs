using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class ActivateProfileCommand : IRequest
{
    public required Guid UserId { get; init; }
}

internal sealed class ActivateProfileCommandHandler : IRequestHandler<ActivateProfileCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly ILogger<ActivateProfileCommandHandler> _logger;

    public ActivateProfileCommandHandler(IConsultantProfileServices profileServices, ILogger<ActivateProfileCommandHandler> logger)
    {
        _profileServices = profileServices;
        _logger = logger;
    }

    public async Task Handle(ActivateProfileCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        if (!profile.IsProfileComplete)
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The consultant profile is not yet complete. The consultant must finish their profile setup before the account can be approved.",
                "admin-action.profile-incomplete");

        profile.IsActive = true;

        await _profileServices.SaveChangesAsync(ct);
        _logger.LogInformation("Consultant profile activated. UserId: {UserId}", command.UserId);
    }
}
