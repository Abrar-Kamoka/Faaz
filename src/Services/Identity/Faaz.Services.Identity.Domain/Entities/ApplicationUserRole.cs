using Microsoft.AspNetCore.Identity;

namespace Faaz.Services.Identity.Domain.Entities;

public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public Guid Id { get; set; }
}
