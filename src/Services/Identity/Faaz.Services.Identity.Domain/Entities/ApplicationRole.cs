using Microsoft.AspNetCore.Identity;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { Id = RT.Comb.Provider.Sql.Create(); }
    public ApplicationRole(string roleName) : base(roleName) { Id = RT.Comb.Provider.Sql.Create(); }

    public UserRole RoleType { get; set; }
}
