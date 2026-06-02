using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Commands;

public class SetUnderReviewCommand : IRequest
{
    public required Guid UserId { get; init; }
}

internal sealed class SetUnderReviewCommandHandler : IRequestHandler<SetUnderReviewCommand>
{
    private readonly IConsultantApplicationServices _appServices;

    public SetUnderReviewCommandHandler(IConsultantApplicationServices appServices)
    {
        _appServices = appServices;
    }

    public async Task Handle(SetUnderReviewCommand command, CancellationToken ct)
    {
        var app = await _appServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantApplication", command.UserId);

        app.ApplicationStatus = ConsultantApplicationStatus.SettingUpProfile;
        await _appServices.SaveChangesAsync(ct);
    }
}
