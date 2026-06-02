using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Queries;

public class GetProfileCompletenessQuery : IRequest<ProfileCompletenessDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetProfileCompletenessQueryHandler : IRequestHandler<GetProfileCompletenessQuery, ProfileCompletenessDto>
{
    private readonly IStudentProfileServices _profileServices;

    public GetProfileCompletenessQueryHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task<ProfileCompletenessDto> Handle(GetProfileCompletenessQuery query, CancellationToken ct)
    {
        var p = await _profileServices.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", query.UserId);

        var steps = new Dictionary<string, bool>
        {
            ["Personal"] = !string.IsNullOrWhiteSpace(p.FirstName) && !string.IsNullOrWhiteSpace(p.FirstLanguage),
            ["StudyLevel"] = p.StudyTrack.HasValue,
            ["Goals"] = p.TargetStudyLevel.HasValue && p.HelpTypes != 0,
            ["Bio"] = !string.IsNullOrWhiteSpace(p.Bio)
        };

        var completed = steps.Where(s => s.Value).Select(s => s.Key).ToArray();
        var missing = steps.Where(s => !s.Value).Select(s => s.Key).ToArray();
        var percentage = (int)Math.Round(completed.Length * 100.0 / steps.Count);

        return new ProfileCompletenessDto
        {
            Percentage = percentage,
            CompletedSteps = completed,
            MissingSteps = missing
        };
    }
}
