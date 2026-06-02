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

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [MaxLength(500)]
    public string? ExtraField1 { get; set; }

    [MaxLength(500)]
    public string? ExtraField2 { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
