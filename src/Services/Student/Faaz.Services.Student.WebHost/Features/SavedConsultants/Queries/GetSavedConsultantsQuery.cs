using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.SavedConsultants.DTOs;
using Faaz.Services.Student.WebHost.HttpClients;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.SavedConsultants.Queries;

public class GetSavedConsultantsQuery : IRequest<List<SavedConsultantDto>>
{
    public Guid StudentUserId { get; set; }
}

internal sealed class GetSavedConsultantsQueryHandler : IRequestHandler<GetSavedConsultantsQuery, List<SavedConsultantDto>>
{
    private readonly ISavedConsultantServices _savedServices;
    private readonly IConsultantServiceClient _consultantClient;

    public GetSavedConsultantsQueryHandler(ISavedConsultantServices s, IConsultantServiceClient c)
    { _savedServices = s; _consultantClient = c; }

    public async Task<List<SavedConsultantDto>> Handle(GetSavedConsultantsQuery query, CancellationToken ct)
    {
        var saved = await _savedServices.GetByStudentIdAsync(query.StudentUserId, ct);

        // Enrich each saved reference with live consultant data — hydration happens here rather than
        // being cached locally so the list never shows stale price/availability/photo.
        var summaries = await Task.WhenAll(saved.Select(s => _consultantClient.GetProfileSummaryAsync(s.ConsultantUserId, ct)));

        return summaries
            .Where(s => s is not null)
            .Select(s => new SavedConsultantDto
            {
                UserId               = s!.UserId,
                ProfileId            = s.ProfileId,
                DisplayName          = s.DisplayName,
                ProfessionalPhotoUrl = s.ProfessionalPhotoUrl,
                CurrentRole          = s.CurrentRole,
                Institution          = s.Institution,
                IsVerified           = s.IsVerified,
                AverageRating        = s.AverageRating,
                ReviewCount          = s.ReviewCount,
                SubjectIds           = s.SubjectIds,
                SessionTypes         = s.SessionTypes.Select(t => new SessionTypeSummaryDto
                {
                    Id              = t.Id,
                    Name            = t.Name,
                    DurationMinutes = t.DurationMinutes,
                    PriceGbp        = t.PriceGbp
                }).ToList(),
                IsAvailableThisWeek = s.IsAvailableThisWeek
            })
            .ToList();
    }
}
