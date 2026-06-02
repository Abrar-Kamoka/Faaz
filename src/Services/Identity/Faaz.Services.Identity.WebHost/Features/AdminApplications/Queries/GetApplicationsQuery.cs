using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using MediatR;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Queries;

public class GetApplicationsQuery : IRequest<PagedResult<ApplicationSummaryDto>>
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

internal sealed class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, PagedResult<ApplicationSummaryDto>>
{
    private readonly IConsultantServiceClient _consultantClient;

    public GetApplicationsQueryHandler(IConsultantServiceClient consultantClient)
    {
        _consultantClient = consultantClient;
    }

    public async Task<PagedResult<ApplicationSummaryDto>> Handle(GetApplicationsQuery query, CancellationToken ct)
    {
        return await _consultantClient.GetApplicationsAsync(query.Status, query.Page, query.PageSize, ct);
    }
}
