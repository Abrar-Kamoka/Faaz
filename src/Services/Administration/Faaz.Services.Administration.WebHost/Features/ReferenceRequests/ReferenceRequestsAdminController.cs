using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using Faaz.SharedKernel.Results;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.ReferenceRequests;

// The "can't find it? request it" queue's admin side. Approving a University/Subject/Service
// request auto-creates the real (initially inactive, pending a proper look) entity, since those
// only need a Name to exist. A Programme request can't be auto-created — it needs a real
// UniversityId/StudyLevel/etc. the free-text request never captured — so approving one just marks
// it Approved; the admin builds the actual Programme via ProgrammesAdminController, referencing
// this request's ProposedName/Details for context.
[Route("api/v1/admin/reference-requests")]
[Authorize(Policy = "AdminOnly")]
public class ReferenceRequestsAdminController(
    IReferenceDataRequestServices requests,
    IUniversityServices universities,
    ISubjectServices subjects,
    IServiceCatalogServices services,
    IAdminActionLogServices auditLog,
    IPublishEndpoint bus) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ReferenceRequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await requests.GetPagedAsync(status, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var r = await requests.GetByIdAsync(id, ct);
        if (r is null) return NotFound(ApiResponse.Fail(404, "Reference data request not found."));
        return Ok(ApiResponse.Ok(r));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewReferenceRequestRequest? req, CancellationToken ct = default)
    {
        var r = await requests.GetByIdAsync(id, ct);
        if (r is null) return NotFound(ApiResponse.Fail(404, "Reference data request not found."));
        if (r.Status != ReferenceRequestStatus.Pending) return BadRequest(ApiResponse.Fail(400, "Request has already been reviewed."));

        var adminId = GetUserId();
        Guid? createdEntityId = null;

        switch (r.EntityType)
        {
            case ReferenceEntityType.University:
            {
                var srNo   = await universities.NewSerialNumberAsync(ct);
                var entity = new University { SrNo = srNo, Name = r.ProposedName, IsActive = false, DataSource = "Admin — Approved Request" };
                await universities.AddAsync(entity, ct);
                await universities.SaveChangesAsync(ct);
                createdEntityId = entity.Id;
                break;
            }
            case ReferenceEntityType.Subject:
            {
                var srNo   = await subjects.NewSerialNumberAsync(ct);
                var entity = new Subject { SrNo = srNo, Name = r.ProposedName, IsActive = false, DataSource = "Admin — Approved Request" };
                await subjects.AddAsync(entity, ct);
                await subjects.SaveChangesAsync(ct);
                createdEntityId = entity.Id;
                break;
            }
            case ReferenceEntityType.Service:
            {
                var srNo   = await services.NewSerialNumberAsync(ct);
                var entity = new Service { SrNo = srNo, Name = r.ProposedName, IsActive = false };
                await services.AddAsync(entity, ct);
                await services.SaveChangesAsync(ct);
                createdEntityId = entity.Id;
                break;
            }
            case ReferenceEntityType.Programme:
                // Not auto-created — see class-level comment. Admin adds it via ProgrammesAdminController.
                break;
        }

        r.Status                = ReferenceRequestStatus.Approved;
        r.ReviewedByAdminUserId = adminId;
        r.ReviewNotes           = req?.Notes;
        r.ReviewedAt            = DateTime.UtcNow;
        r.CreatedEntityId       = createdEntityId;
        await requests.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.ApproveReferenceRequest, r.Id, r.ProposedName, ct);
        await bus.Publish(new ReferenceRequestApprovedEvent(r.RequestedByUserId, r.EntityType.ToString(), r.ProposedName), ct);

        return Ok(ApiResponse.Ok(new { CreatedEntityId = createdEntityId }));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewReferenceRequestRequest req, CancellationToken ct = default)
    {
        var r = await requests.GetByIdAsync(id, ct);
        if (r is null) return NotFound(ApiResponse.Fail(404, "Reference data request not found."));
        if (r.Status != ReferenceRequestStatus.Pending) return BadRequest(ApiResponse.Fail(400, "Request has already been reviewed."));

        var adminId              = GetUserId();
        r.Status                = ReferenceRequestStatus.Rejected;
        r.ReviewedByAdminUserId = adminId;
        r.ReviewNotes           = req.Notes;
        r.ReviewedAt            = DateTime.UtcNow;
        await requests.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.RejectReferenceRequest, r.Id, r.ProposedName, ct);
        await bus.Publish(new ReferenceRequestRejectedEvent(r.RequestedByUserId, r.EntityType.ToString(), r.ProposedName, req.Notes), ct);

        return Ok(ApiResponse.NoContent("Request rejected."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "ReferenceDataRequest",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record ReviewReferenceRequestRequest(string? Notes = null);
