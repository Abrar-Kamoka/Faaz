using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using Faaz.SharedKernel.Results;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Users;

[Route("api/v1/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersAdminController(
    IAdminIdentityClient identityClient,
    IAdminActionLogServices auditLog,
    IPublishEndpoint bus) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await identityClient.GetUsersAsync(page, pageSize, search, role, isActive, ct);
        if (result is null)
            return Problem("Identity service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct = default)
    {
        var result = await identityClient.GetUserByIdAsync(userId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "User not found"));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(
        Guid userId,
        [FromBody] DeactivateUserRequest req,
        CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok = await identityClient.DeactivateUserAsync(userId, req.Reason, adminId, ct);
        if (!ok)
            return Problem("Failed to deactivate user", statusCode: 502);

        await WriteAuditLogAsync(adminId, AdminAction.DeactivateUser, "User", userId, req.Reason, ct);
        await bus.Publish(new UserDeactivatedByAdminEvent(userId, adminId, req.Reason), ct);

        return Ok(ApiResponse.NoContent("User deactivated."));
    }

    [HttpPost("{userId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid userId, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok = await identityClient.ReactivateUserAsync(userId, adminId, ct);
        if (!ok)
            return Problem("Failed to reactivate user", statusCode: 502);

        await WriteAuditLogAsync(adminId, AdminAction.ReactivateUser, "User", userId, null, ct);
        await bus.Publish(new UserReactivatedByAdminEvent(userId, adminId), ct);

        return Ok(ApiResponse.NoContent("User reactivated."));
    }

    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("staff.manage") is { } denied) return denied;

        var adminId = GetUserId();
        var user = await identityClient.CreateStaffAsync(
            req.Email, req.FirstName, req.LastName, req.RoleName, adminId, ct);
        if (user is null)
            return Problem("Failed to create staff user", statusCode: 502);

        await WriteAuditLogAsync(adminId, AdminAction.CreateStaff, "User", user.Id, $"Role: {req.RoleName}", ct);
        await bus.Publish(new StaffCreatedEvent(user.Id, adminId, req.RoleName), ct);

        return StatusCode(201, ApiResponse.Created(new { user.Id }));
    }

    private async Task WriteAuditLogAsync(
        Guid adminId, AdminAction action, string entityType, Guid entityId,
        string? notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record DeactivateUserRequest(string Reason);
public record CreateStaffRequest(string Email, string FirstName, string LastName, string RoleName);
