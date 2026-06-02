using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Commands;

public class PreApproveCommand : IRequest<string>
{
    public required Guid ApplicationId { get; init; }
    public AdminNoteDto PostModel { get; set; } = null!;
}

internal sealed class PreApproveCommandHandler : IRequestHandler<PreApproveCommand, string>
{
    private readonly IConsultantApplicationServices _appServices;
    private readonly ILogger<PreApproveCommandHandler> _logger;

    public PreApproveCommandHandler(IConsultantApplicationServices appServices, ILogger<PreApproveCommandHandler> logger)
    {
        _appServices = appServices;
        _logger = logger;
    }

    public async Task<string> Handle(PreApproveCommand command, CancellationToken ct)
    {
        var app = await _appServices.GetByIdAsync(command.ApplicationId, ct)
            ?? throw new NotFoundException("ConsultantApplication", command.ApplicationId);

        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var plaintext = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(bytes);
        var hash = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext)));

        app.ApplicationStatus = ConsultantApplicationStatus.Invited;
        app.AdminNotes = command.PostModel.Notes;
        app.SetupInviteToken = hash;
        app.SetupInviteTokenExpiry = DateTime.UtcNow.AddHours(72);
        app.SetupInviteSentAt = DateTime.UtcNow;

        await _appServices.SaveChangesAsync(ct);
        _logger.LogInformation("Application {Id} pre-approved", command.ApplicationId);

        return plaintext;
    }
}
