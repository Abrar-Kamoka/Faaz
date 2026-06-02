using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Commands;

public class LinkUserToApplicationCommand : IRequest
{
    public LinkUserDto PostModel { get; set; } = null!;
}

internal sealed class LinkUserToApplicationCommandHandler : IRequestHandler<LinkUserToApplicationCommand>
{
    private readonly IConsultantApplicationServices _appServices;

    public LinkUserToApplicationCommandHandler(IConsultantApplicationServices appServices)
    {
        _appServices = appServices;
    }

    public async Task Handle(LinkUserToApplicationCommand command, CancellationToken ct)
    {
        var application = await _appServices.GetByIdAsync(command.PostModel.ApplicationId, ct)
            ?? throw new NotFoundException("ConsultantApplication", command.PostModel.ApplicationId);

        application.UserId = command.PostModel.UserId;
        await _appServices.SaveChangesAsync(ct);
    }
}
