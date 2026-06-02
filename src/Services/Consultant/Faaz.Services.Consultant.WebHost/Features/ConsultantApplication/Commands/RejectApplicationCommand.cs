using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Commands;

public class RejectApplicationCommand : IRequest
{
    public required Guid ApplicationId { get; init; }
    public AdminNoteDto PostModel { get; set; } = null!;
}

internal sealed class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand>
{
    private readonly IConsultantApplicationServices _appServices;

    public RejectApplicationCommandHandler(IConsultantApplicationServices appServices)
    {
        _appServices = appServices;
    }

    public async Task Handle(RejectApplicationCommand command, CancellationToken ct)
    {
        var app = await _appServices.GetByIdAsync(command.ApplicationId, ct)
            ?? throw new NotFoundException("ConsultantApplication", command.ApplicationId);

        app.ApplicationStatus = ConsultantApplicationStatus.Rejected;
        app.AdminNotes = command.PostModel.Notes;
        await _appServices.SaveChangesAsync(ct);
    }
}
