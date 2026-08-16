using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.SavedConsultants.Commands;

public class SaveConsultantCommand : IRequest
{
    public Guid StudentUserId { get; set; }
    public Guid ConsultantUserId { get; set; }
}

internal sealed class SaveConsultantCommandHandler : IRequestHandler<SaveConsultantCommand>
{
    private readonly ISavedConsultantServices _savedServices;
    private readonly IConsultantServiceClient _consultantClient;

    public SaveConsultantCommandHandler(ISavedConsultantServices s, IConsultantServiceClient c)
    { _savedServices = s; _consultantClient = c; }

    public async Task Handle(SaveConsultantCommand command, CancellationToken ct)
    {
        if (await _savedServices.ExistsAsync(command.StudentUserId, command.ConsultantUserId, ct))
            return; // idempotent — saving an already-saved consultant is a no-op, not an error

        var profile = await _consultantClient.GetProfileSummaryAsync(command.ConsultantUserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.ConsultantUserId);

        await _savedServices.AddAsync(new SavedConsultant
        {
            StudentUserId    = command.StudentUserId,
            ConsultantUserId = profile.UserId
        }, ct);
        await _savedServices.SaveChangesAsync(ct);
    }
}
