using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using MediatR;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Queries;

public class GetApplicationDetailQuery : IRequest<ApplicationDetailDto>
{
    public Guid ApplicationId { get; set; }
}

internal sealed class GetApplicationDetailQueryHandler : IRequestHandler<GetApplicationDetailQuery, ApplicationDetailDto>
{
    private readonly IConsultantServiceClient _consultantClient;

    public GetApplicationDetailQueryHandler(IConsultantServiceClient consultantClient)
    {
        _consultantClient = consultantClient;
    }

    public async Task<ApplicationDetailDto> Handle(GetApplicationDetailQuery query, CancellationToken ct)
    {
        return await _consultantClient.GetApplicationDetailAsync(query.ApplicationId, ct);
    }
}
