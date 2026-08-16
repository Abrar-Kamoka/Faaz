using Faaz.Services.Student.Infrastructure.Interfaces;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.SavedConsultants.Commands;

public class UnsaveConsultantCommand : IRequest
{
    public Guid StudentUserId { get; set; }
    public Guid ConsultantUserId { get; set; }
}

internal sealed class UnsaveConsultantCommandHandler : IRequestHandler<UnsaveConsultantCommand>
{
    private readonly ISavedConsultantServices _savedServices;

    public UnsaveConsultantCommandHandler(ISavedConsultantServices s) { _savedServices = s; }

    public async Task Handle(UnsaveConsultantCommand command, CancellationToken ct)
    {
        var removed = await _savedServices.RemoveAsync(command.StudentUserId, command.ConsultantUserId, ct);
        if (removed) await _savedServices.SaveChangesAsync(ct);
        // Removing something already-unsaved is a no-op, not an error — idempotent by design.
    }
}
