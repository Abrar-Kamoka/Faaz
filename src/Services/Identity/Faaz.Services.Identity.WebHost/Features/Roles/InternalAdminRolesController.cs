using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.WebHost.Features.Roles;

[Route("internal/admin")]
[Tags("Internal - Admin")]
[IgnoreAntiforgeryToken]
public class InternalAdminRolesController : FaazApiController
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;

    public InternalAdminRolesController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IdentityDbContext db,
        IConfiguration config)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db          = db;
        _config      = config;
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var items = await _db.Permissions
            .OrderBy(p => p.Category).ThenBy(p => p.Key)
            .Select(p => new { p.Id, p.Key, p.Category, p.Description })
            .ToListAsync();

        return Ok(ApiResponse.Ok(items));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var roles = await _roleManager.Roles
            .Where(r => r.RoleType == UserRole.SuperAdmin)
            .OrderByDescending(r => r.IsSystemRole).ThenBy(r => r.Name)
            .ToListAsync();

        var items = new List<object>();
        foreach (var role in roles)
        {
            var claims      = await _roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
            var memberCount = await _userManager.GetUsersInRoleAsync(role.Name!);

            items.Add(new
            {
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                Permissions = permissions,
                MemberCount = memberCount.Count
            });
        }

        return Ok(ApiResponse.Ok(items));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] SaveRoleBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        if (await _roleManager.RoleExistsAsync(req.Name))
            return BadRequest(ApiResponse.Fail(400, "A role with this name already exists."));

        var role = new ApplicationRole(req.Name)
        {
            RoleType     = UserRole.SuperAdmin,
            IsSystemRole = false,
            Description  = req.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(ApiResponse.Fail(400, string.Join(", ", result.Errors.Select(e => e.Description))));

        await SetPermissionsAsync(role, req.PermissionKeys);

        return StatusCode(201, ApiResponse.Created(new { role.Id }));
    }

    [HttpPut("roles/{roleId:guid}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] SaveRoleBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return NotFound(ApiResponse.Fail(404, "Role not found."));
        if (role.IsSystemRole) return BadRequest(ApiResponse.Fail(400, "Built-in roles can't be modified."));

        role.Name        = req.Name;
        role.Description = req.Description;
        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
            return BadRequest(ApiResponse.Fail(400, string.Join(", ", updateResult.Errors.Select(e => e.Description))));

        await SetPermissionsAsync(role, req.PermissionKeys);

        return Ok(ApiResponse.NoContent("Role updated."));
    }

    [HttpDelete("roles/{roleId:guid}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return NotFound(ApiResponse.Fail(404, "Role not found."));
        if (role.IsSystemRole) return BadRequest(ApiResponse.Fail(400, "Built-in roles can't be deleted."));

        var members = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (members.Count > 0)
            return BadRequest(ApiResponse.Fail(400, $"Reassign the {members.Count} staff member(s) on this role before deleting it."));

        await _roleManager.DeleteAsync(role);
        return Ok(ApiResponse.NoContent("Role deleted."));
    }

    [HttpPost("users/{userId:guid}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(ApiResponse.Fail(404, "User not found."));
        if (user.Role != UserRole.SuperAdmin) return BadRequest(ApiResponse.Fail(400, "Only staff (SuperAdmin) accounts can be assigned an admin role."));

        var newRole = await _roleManager.FindByIdAsync(req.RoleId.ToString());
        if (newRole is null || newRole.RoleType != UserRole.SuperAdmin)
            return NotFound(ApiResponse.Fail(404, "Role not found."));

        // Single-role-at-a-time for staff — swapping roles replaces access rather than adding to it,
        // which is the model the frontend's single-select role picker assumes.
        var currentRoles = (await _userManager.GetRolesAsync(user))
            .Where(r => r != newRole.Name)
            .ToList();
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!await _userManager.IsInRoleAsync(user, newRole.Name!))
            await _userManager.AddToRoleAsync(user, newRole.Name!);

        return Ok(ApiResponse.NoContent("Role assigned."));
    }

    private async Task SetPermissionsAsync(ApplicationRole role, IReadOnlyList<string> permissionKeys)
    {
        var existing = (await _roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == "permission")
            .ToList();

        foreach (var claim in existing)
            if (!permissionKeys.Contains(claim.Value))
                await _roleManager.RemoveClaimAsync(role, claim);

        var existingValues = existing.Select(c => c.Value).ToHashSet();
        foreach (var key in permissionKeys)
            if (!existingValues.Contains(key))
                await _roleManager.AddClaimAsync(role, new Claim("permission", key));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record SaveRoleBody(string Name, string? Description, List<string> PermissionKeys);
public record AssignRoleBody(Guid RoleId);
