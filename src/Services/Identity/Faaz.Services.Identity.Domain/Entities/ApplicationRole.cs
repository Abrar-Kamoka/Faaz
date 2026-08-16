using Microsoft.AspNetCore.Identity;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { Id = RT.Comb.Provider.Sql.Create(); }
    public ApplicationRole(string roleName) : base(roleName) { Id = RT.Comb.Provider.Sql.Create(); }

    public UserRole RoleType { get; set; }

    // The 3 built-in roles (Student/Consultant/Admin) — can't be deleted or renamed via the admin
    // Roles Management screen. Custom roles created there (e.g. "Support Agent") are RoleType == Admin
    // but IsSystemRole == false, and carry permission claims (see IdentityRoleClaim, claim type "permission").
    public bool IsSystemRole { get; set; }
    public string? Description { get; set; }
}
