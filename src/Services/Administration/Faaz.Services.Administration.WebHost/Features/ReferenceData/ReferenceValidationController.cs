using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.ReferenceData;

// Service-to-service only (X-Service-Key). This is what stops a consultant/student from ever
// persisting a University/Programme/Subject/Service Guid that isn't a real, active catalog row —
// Consultant's and Student's UpdateExpertise/UpdateGoals command handlers call this before saving.
[Route("internal/reference")]
[Tags("Internal - Reference Data")]
[IgnoreAntiforgeryToken]
public class ReferenceValidationController(
    IUniversityServices universities,
    IProgrammeServices programmes,
    ISubjectServices subjects,
    IServiceCatalogServices services,
    IConfiguration config) : AdminInternalController(config)
{
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateReferenceIdsRequest req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var invalidUniversities = new List<Guid>();
        foreach (var id in req.UniversityIds ?? [])
        {
            var u = await universities.GetByIdAsync(id, ct);
            if (u is null || !u.IsActive) invalidUniversities.Add(id);
        }

        var invalidProgrammes = new List<Guid>();
        foreach (var id in req.ProgrammeIds ?? [])
        {
            var p = await programmes.GetByIdAsync(id, ct);
            if (p is null || !p.IsActive) invalidProgrammes.Add(id);
        }

        var invalidSubjects = new List<Guid>();
        foreach (var id in req.SubjectIds ?? [])
        {
            var s = await subjects.GetByIdAsync(id, ct);
            if (s is null || !s.IsActive) invalidSubjects.Add(id);
        }

        var invalidServices = new List<Guid>();
        foreach (var id in req.ServiceIds ?? [])
        {
            var svc = await services.GetByIdAsync(id, ct);
            if (svc is null || !svc.IsActive) invalidServices.Add(id);
        }

        return Ok(ApiResponse.Ok(new ValidateReferenceIdsResponse(
            invalidUniversities.ToArray(), invalidProgrammes.ToArray(), invalidSubjects.ToArray(), invalidServices.ToArray())));
    }
}

public record ValidateReferenceIdsRequest(Guid[]? UniversityIds, Guid[]? ProgrammeIds, Guid[]? SubjectIds, Guid[]? ServiceIds);
public record ValidateReferenceIdsResponse(Guid[] InvalidUniversityIds, Guid[] InvalidProgrammeIds, Guid[] InvalidSubjectIds, Guid[] InvalidServiceIds);
