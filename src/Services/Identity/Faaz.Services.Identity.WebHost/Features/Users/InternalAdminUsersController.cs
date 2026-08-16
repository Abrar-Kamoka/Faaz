using Faaz.Services.Identity.Domain;
using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.WebHost.Features.Users;

[Route("internal/admin")]
[Tags("Internal - Admin")]
[IgnoreAntiforgeryToken]
public class InternalAdminUsersController : FaazApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public InternalAdminUsersController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config      = config;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var query = _userManager.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Email!.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));

        if (!string.IsNullOrEmpty(role) && int.TryParse(role, out var roleInt))
            query = query.Where(u => (int)u.Role == roleInt);

        if (isActive.HasValue)
            query = isActive.Value
                ? query.Where(u => u.Status == UserStatus.Active)
                : query.Where(u => u.Status != UserStatus.Active);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Only Admin-tier accounts have a meaningful RBAC role assignment — skip the extra
        // UserManager round trip for Student/Consultant rows.
        var items = new List<object>();
        foreach (var u in users)
        {
            var roleNames = u.Role == UserRole.Admin ? await _userManager.GetRolesAsync(u) : [];
            items.Add(new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                Role        = (int)u.Role,
                RoleNames   = roleNames,
                IsActive    = u.Status == UserStatus.Active,
                CreatedAt   = u.CreatedAt ?? DateTime.MinValue,
                PhoneNumber = u.PhoneNumber
            });
        }

        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total }));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return NotFound(ApiResponse.Fail(404, "User not found."));

        return Ok(ApiResponse.Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            Role        = (int)user.Role,
            IsActive    = user.Status == UserStatus.Active,
            CreatedAt   = user.CreatedAt ?? DateTime.MinValue,
            user.PhoneNumber
        }));
    }

    [HttpPost("users/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid userId, [FromBody] AdminActionBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return NotFound(ApiResponse.Fail(404, "User not found."));

        user.Status   = UserStatus.Suspended;
        user.Remarks  = req.Reason;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = req.AdminId;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse.NoContent("User deactivated."));
    }

    [HttpPost("users/{userId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid userId, [FromBody] AdminActionBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return NotFound(ApiResponse.Fail(404, "User not found."));

        user.Status    = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = req.AdminId;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse.NoContent("User reactivated."));
    }

    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffBody req)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing is not null)
            return BadRequest(ApiResponse.Fail(400, "Email already in use."));

        var staffRole = req.RoleName.ToLower() switch
        {
            "admin" => UserRole.Admin,
            _ => UserRole.Admin
        };

        var maxSrNo = await _userManager.Users.MaxAsync(u => (int?)u.SrNo);
        var user = new ApplicationUser
        {
            SrNo       = (maxSrNo ?? 0) + 1,
            UserName   = req.Email,
            Email      = req.Email,
            FirstName  = req.FirstName,
            LastName   = req.LastName,
            Role       = staffRole,
            Status     = UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = req.CreatedByAdminId
        };

        var result = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "Aa1!");
        if (!result.Succeeded)
            return BadRequest(ApiResponse.Fail(400, string.Join(", ", result.Errors.Select(e => e.Description))));

        // Defaults new staff to the built-in Admin role (full access) — matches today's "any admin
        // can do anything" behaviour. An admin can later move them to a narrower custom role from
        // Roles Management without this ever needing to change.
        await _userManager.AddToRoleAsync(user, nameof(UserRole.Admin));

        return StatusCode(201, ApiResponse.Created(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            Role     = (int)user.Role,
            IsActive = true,
            CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
            PhoneNumber = (string?)null
        }));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record AdminActionBody(string? Reason, Guid AdminId);
public record CreateStaffBody(string Email, string FirstName, string LastName, string RoleName, Guid CreatedByAdminId);
