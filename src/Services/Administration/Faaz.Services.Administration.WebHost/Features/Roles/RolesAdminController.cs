using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Roles;

[Route("api/v1/admin/roles")]
[Authorize(Policy = "AdminOnly")]
public class RolesAdminController(
    IAdminIdentityClient identityClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken ct = default)
    {
        var result = await identityClient.GetPermissionsAsync(ct);
        if (result is null) return Problem("Identity service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct = default)
    {
        var result = await identityClient.GetRolesAsync(ct);
        if (result is null) return Problem("Identity service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] SaveRoleRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("staff.manage") is { } denied) return denied;

        var roleId = await identityClient.CreateRoleAsync(req.Name, req.Description, req.PermissionKeys, ct);
        if (roleId is null) return Problem("Failed to create role — the name may already be in use.", statusCode: 502);

        var adminId = GetUserId();
        await WriteAuditAsync(adminId, AdminAction.CreateRole, "Role", roleId.Value, req.Name, ct);
        return StatusCode(201, ApiResponse.Created(new { Id = roleId }));
    }

    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] SaveRoleRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("staff.manage") is { } denied) return denied;

        var ok = await identityClient.UpdateRoleAsync(roleId, req.Name, req.Description, req.PermissionKeys, ct);
        if (!ok) return Problem("Failed to update role.", statusCode: 502);

        var adminId = GetUserId();
        await WriteAuditAsync(adminId, AdminAction.UpdateRole, "Role", roleId, req.Name, ct);
        return Ok(ApiResponse.NoContent("Role updated."));
    }

    [HttpDelete("{roleId:guid}")]
    public async Task<IActionResult> DeleteRole(Guid roleId, CancellationToken ct = default)
    {
        if (RequirePermission("staff.manage") is { } denied) return denied;

        var (success, error) = await identityClient.DeleteRoleAsync(roleId, ct);
        if (!success) return UnprocessableEntity(ApiResponse.Fail(422, error ?? "Failed to delete role."));

        var adminId = GetUserId();
        await WriteAuditAsync(adminId, AdminAction.DeleteRole, "Role", roleId, null, ct);
        return Ok(ApiResponse.NoContent("Role deleted."));
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("staff.manage") is { } denied) return denied;

        var ok = await identityClient.AssignRoleAsync(req.UserId, req.RoleId, ct);
        if (!ok) return Problem("Failed to assign role.", statusCode: 502);

        var adminId = GetUserId();
        await WriteAuditAsync(adminId, AdminAction.UpdateStaffRole, "User", req.UserId, $"Role: {req.RoleId}", ct);
        return Ok(ApiResponse.NoContent("Role assigned."));
    }

    private async Task WriteAuditAsync(Guid adminId, AdminAction action, string entityType, Guid entityId, string? notes, CancellationToken ct)
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

public record SaveRoleRequest(string Name, string? Description, List<string> PermissionKeys);
public record AssignRoleRequest(Guid UserId, Guid RoleId);
