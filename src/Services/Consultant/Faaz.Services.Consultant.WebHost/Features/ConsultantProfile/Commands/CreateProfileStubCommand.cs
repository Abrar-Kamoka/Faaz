using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class CreateProfileStubCommand : IRequest
{
    public required Guid UserId { get; init; }
    /// <summary>
    /// Fallback when the application's UserId link was never set (e.g. Consultant service was
    /// down during account creation). Allows lookup by email so the stub can still be created.
    /// </summary>
    public string? Email { get; init; }
}

internal sealed class CreateProfileStubCommandHandler : IRequestHandler<CreateProfileStubCommand>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IConsultantApplicationServices _applicationServices;
    private readonly ILogger<CreateProfileStubCommandHandler> _logger;

    public CreateProfileStubCommandHandler(
        IConsultantProfileServices profileServices,
        IConsultantApplicationServices applicationServices,
        ILogger<CreateProfileStubCommandHandler> logger)
    {
        _profileServices = profileServices;
        _applicationServices = applicationServices;
        _logger = logger;
    }

    public async Task Handle(CreateProfileStubCommand command, CancellationToken ct)
    {
        if (await _profileServices.ExistsForUserAsync(command.UserId, ct))
            return;

        // Try by UserId first (fast path). Fall back to email if the link was never persisted.
        var application = await _applicationServices.GetByUserIdAsync(command.UserId, ct);

        if (application is null && !string.IsNullOrWhiteSpace(command.Email))
        {
            application = await _applicationServices.GetByEmailAsync(command.Email, ct);
            if (application is not null)
            {
                // Repair the missing link so future lookups succeed.
                application.UserId = command.UserId;
                await _applicationServices.SaveChangesAsync(ct);
                _logger.LogWarning(
                    "Repaired missing UserId link on ConsultantApplication {ApplicationId} for user {UserId}",
                    application.Id, command.UserId);
            }
        }

        if (application is null)
            throw new NotFoundException("ConsultantApplication", command.UserId);

        // Pre-fill from the EoI so the consultant doesn't re-enter data they already provided.
        var profile = ConsultantProfileStubBuilder.Build(command.UserId, application);

        await _profileServices.AddAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);

        _logger.LogInformation("Consultant profile stub created. UserId: {UserId}", command.UserId);
    }
}
