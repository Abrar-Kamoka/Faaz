using Microsoft.AspNetCore.Identity;

namespace Faaz.Services.Identity.Domain.Entities;

public class ApplicationUserToken : IdentityUserToken<Guid>
{
    public Guid Id { get; set; }
}
