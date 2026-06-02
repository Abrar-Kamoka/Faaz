using Faaz.Services.Identity.Infrastructure.Interfaces.Users;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class RegisterConsultantCommand : IRequest<Guid>
{
    public RegisterConsultantDto PostModel { get; set; } = null!;
}

internal sealed class RegisterConsultantCommandHandler : IRequestHandler<RegisterConsultantCommand, Guid>
{
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IApplicationUserRepository _userRepo;
    private readonly ILogger<RegisterConsultantCommandHandler> _logger;

    public RegisterConsultantCommandHandler(
        IConsultantServiceClient consultantClient,
        IApplicationUserRepository userRepo,
        ILogger<RegisterConsultantCommandHandler> logger)
    {
        _consultantClient = consultantClient;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterConsultantCommand command, CancellationToken ct)
    {
        var dto = command.PostModel;

        // Guard: reject if email or phone is already associated with any account.
        if (await _userRepo.GetByEmailAsync(dto.Email, ct) is not null)
            throw new ConflictException("An account with this email address already exists.");

        if (await _userRepo.GetByPhoneNumberAsync(dto.PhoneNumber, ct) is not null)
            throw new ConflictException("An account with this phone number already exists.");

        // Only build document list when both files and their types are provided.
        // If no files selected → DocumentTypes is ignored entirely (no error).
        var documents = dto.Files?.Count > 0 && dto.DocumentTypes is { Count: > 0 }
            ? dto.Files
                .Select((file, i) => new DocumentSubmission(
                    file,
                    dto.DocumentTypes.Count > i && dto.DocumentTypes[i].HasValue
                        ? dto.DocumentTypes[i]!.Value
                        : Faaz.SharedKernel.SharedEnums.ApplicationDocumentType.OtherSupportingDocument))
                .ToList()
            : null;

        var request = new ApplicationSubmissionRequest
        {
            Email              = dto.Email,
            FirstName          = dto.FirstName,
            LastName           = dto.LastName,
            PhoneNumber        = dto.PhoneNumber,
            IsUkBased          = dto.IsUkBased,
            CurrentRole        = dto.CurrentRole,
            ExpertiseArea      = dto.ExpertiseArea,
            YearsOfExperience  = dto.YearsOfExperience,
            DateOfBirth        = dto.DateOfBirth,
            Nationality        = dto.Nationality,
            CountryOfResidence = dto.CountryOfResidence,
            LinkedInProfileUrl = dto.LinkedInProfileUrl,
            HighestQualification = dto.HighestQualification,
            PrimaryLanguage    = dto.PrimaryLanguage,
            PersonalStatement  = dto.PersonalStatement,
            ConsultationMode   = dto.ConsultationMode,
            Documents          = documents
        };

        var applicationId = await _consultantClient.InitialiseApplicationAsync(request, ct);
        _logger.LogInformation("Consultant EoI submitted. ApplicationId: {ApplicationId}", applicationId);
        return applicationId;
    }
}
