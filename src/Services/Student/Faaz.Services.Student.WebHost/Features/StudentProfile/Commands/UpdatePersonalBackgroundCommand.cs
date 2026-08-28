using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.Services.Student.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdatePersonalBackgroundCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdatePersonalBackgroundDto PutModel { get; set; } = null!;
}

internal sealed class UpdatePersonalBackgroundCommandHandler : IRequestHandler<UpdatePersonalBackgroundCommand>
{
    private readonly IStudentProfileServices _profileServices;
    private readonly IIdentityServiceClient _identityClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdatePersonalBackgroundCommandHandler> _logger;

    public UpdatePersonalBackgroundCommandHandler(
        IStudentProfileServices profileServices,
        IIdentityServiceClient identityClient,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdatePersonalBackgroundCommandHandler> logger)
    {
        _profileServices = profileServices;
        _identityClient  = identityClient;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Handle(UpdatePersonalBackgroundCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        var wasComplete = profile.IsOnboardingComplete;

        profile.FirstName            = command.PutModel.FirstName;
        profile.LastName             = command.PutModel.LastName;
        profile.DateOfBirth          = command.PutModel.DateOfBirth;
        profile.CountryOfCitizenship = command.PutModel.CountryOfCitizenship;
        profile.CountryOfResidence   = command.PutModel.CountryOfResidence;
        profile.Ethnicity            = command.PutModel.Ethnicity;
        profile.FirstLanguage        = command.PutModel.FirstLanguage;
        profile.AdditionalLanguages  = command.PutModel.AdditionalLanguages;
        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);

        if (!wasComplete && profile.IsOnboardingComplete)
            await _publishEndpoint.Publish(new StudentOnboardingCompletedEvent(profile.UserId, profile.FirstName), ct);

        try
        {
            await _identityClient.UpdateUserNameAsync(command.UserId, command.PutModel.FirstName, command.PutModel.LastName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not sync name to Identity service for {UserId}: {Message}", command.UserId, ex.Message);
        }
    }
}
