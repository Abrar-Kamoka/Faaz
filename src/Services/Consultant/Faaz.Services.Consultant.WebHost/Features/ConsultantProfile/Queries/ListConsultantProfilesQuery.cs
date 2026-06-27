using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;

public class ListConsultantProfilesQuery : IRequest<(IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)>
{
    public string? Subject  { get; set; }
    public int     Page     { get; set; } = 1;
    public int     PageSize { get; set; } = 10;
}

internal sealed class ListConsultantProfilesQueryHandler
    : IRequestHandler<ListConsultantProfilesQuery, (IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)>
{
    private readonly IConsultantProfileServices _profileServices;

    public ListConsultantProfilesQueryHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task<(IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)> Handle(
        ListConsultantProfilesQuery query, CancellationToken ct)
    {
        var (profiles, total) = await _profileServices.GetAllActiveAsync(query.Subject, query.Page, query.PageSize, ct);

        var dtos = profiles.Select(p => new ConsultantProfileSummaryDto
        {
            UserId               = p.UserId,
            ProfileId            = p.Id,
            DisplayName          = p.DisplayName,
            CurrentRole          = p.CurrentRole,
            Institution          = p.Institution,
            ProfessionalPhotoUrl = p.ProfessionalPhotoUrl,
            SubjectAreas         = p.SubjectAreas,
            YearsOfExperience    = p.YearsOfExperience,
            MinPriceGbp          = p.SessionTypes.Any() ? p.SessionTypes.Min(s => s.PriceGbp) : 0m,
            SessionTypes         = p.SessionTypes.OrderBy(s => s.SortOrder).Select(s => new SessionTypeSummaryDto
            {
                Id              = s.Id,
                Name            = s.Name,
                DurationMinutes = s.DurationMinutes,
                PriceGbp        = s.PriceGbp
            }).ToList()
        }).ToList();

        return (dtos, total);
    }
}
