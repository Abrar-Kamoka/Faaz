using Faaz.Services.Consultant.Domain;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;

public class GetProfileCompletenessQuery : IRequest<ProfileCompletenessDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetProfileCompletenessQueryHandler : IRequestHandler<GetProfileCompletenessQuery, ProfileCompletenessDto>
{
    private readonly IConsultantProfileServices _profileServices;

    public GetProfileCompletenessQueryHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task<ProfileCompletenessDto> Handle(GetProfileCompletenessQuery query, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdWithCollectionsAsync(query.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", query.UserId);

        var check = ProfileCompletenessChecker.Evaluate(profile);

        return new ProfileCompletenessDto
        {
            Percentage      = check.Percentage,
            HasPersonalInfo = check.HasPersonalInfo,
            HasExpertise    = check.HasExpertise,
            HasBio          = check.HasBio,
            HasPricing      = check.HasPricing,
            HasAvailability = check.HasAvailability,
            IsComplete      = check.IsComplete
        };
    }
}
