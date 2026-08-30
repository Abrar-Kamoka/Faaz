using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.Infrastructure.Services;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using MediatR;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;

public class ListConsultantProfilesQuery : IRequest<(IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)>
{
    public Guid?      SubjectId    { get; set; }
    public string?    Search       { get; set; }
    public Guid?      ServiceId    { get; set; }
    public StudyLevel? StudyLevel  { get; set; }
    public bool?       VerifiedOnly { get; set; }
    public int         Page         { get; set; } = 1;
    public int         PageSize     { get; set; } = 10;
}

internal sealed class ListConsultantProfilesQueryHandler
    : IRequestHandler<ListConsultantProfilesQuery, (IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IBookingReviewClient _reviewClient;

    public ListConsultantProfilesQueryHandler(IConsultantProfileServices profileServices, IBookingReviewClient reviewClient)
    {
        _profileServices = profileServices;
        _reviewClient = reviewClient;
    }

    public async Task<(IReadOnlyList<ConsultantProfileSummaryDto> Items, int Total)> Handle(
        ListConsultantProfilesQuery query, CancellationToken ct)
    {
        var (profiles, total) = await _profileServices.GetAllActiveAsync(
            query.SubjectId, query.Search, query.ServiceId, query.StudyLevel, query.VerifiedOnly,
            query.Page, query.PageSize, ct);

        // Consultant doesn't own review data — one best-effort live lookup per profile on this
        // page, in parallel; a failed lookup just leaves that profile's rating at its zero default.
        var summaries = await Task.WhenAll(profiles.Select(p => _reviewClient.GetReviewSummaryAsync(p.Id, ct)));
        var summaryByProfileId = profiles
            .Zip(summaries, (p, s) => (p.Id, Summary: s))
            .ToDictionary(x => x.Id, x => x.Summary);

        var dtos = profiles.Select(p => new ConsultantProfileSummaryDto
        {
            UserId               = p.UserId,
            ProfileId            = p.Id,
            DisplayName          = p.DisplayName,
            CurrentRole          = p.CurrentRole,
            Institution          = p.Institution,
            ProfessionalPhotoUrl = p.ProfessionalPhotoUrl,
            SubjectIds           = p.Subjects.Select(s => s.SubjectId).ToArray(),
            YearsOfExperience    = p.YearsOfExperience,
            MinPriceGbp          = p.SessionTypes.Any() ? p.SessionTypes.Min(s => s.PriceGbp) : 0m,
            IsVerified           = p.IsFeatured,
            AverageRating        = summaryByProfileId[p.Id]?.AverageRating ?? 0m,
            ReviewCount          = summaryByProfileId[p.Id]?.TotalCount ?? 0,
            IsAvailableThisWeek  = p.AvailabilitySlots.Any(),
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
