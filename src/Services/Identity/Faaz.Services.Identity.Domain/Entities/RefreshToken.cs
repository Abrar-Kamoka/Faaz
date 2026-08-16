using System.ComponentModel.DataAnnotations;
using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Identity.Domain.Entities;

public class RefreshToken : BaseSoftDeleteModel
{
    public RefreshToken()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid UserId { get; set; }

    [MaxLength(88)]
    public required string Token { get; set; }

    [MaxLength(36)]
    public required string JwtId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsRevoked { get; set; } = false;

    /// <summary>Set from the login "Remember me" checkbox — controls both cookie persistence
    /// and how long each rotation on refresh extends ExpiresAt (30 days vs 1 day).</summary>
    public bool RememberMe { get; set; } = false;

    public string? ReplacedByToken { get; set; }

    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
