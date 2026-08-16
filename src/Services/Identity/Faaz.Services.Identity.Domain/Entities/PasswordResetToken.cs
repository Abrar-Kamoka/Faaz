using System.ComponentModel.DataAnnotations;
using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Identity.Domain.Entities;

public class PasswordResetToken : BaseSoftDeleteModel
{
    public PasswordResetToken()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid UserId { get; set; }

    [MaxLength(88)]
    public required string Token { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public ApplicationUser User { get; set; } = null!;
}
