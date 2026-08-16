using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    // Set by application code via NewSerialNumberAsync() (MAX+1). ValueGeneratedNever() in IdentityDbContext.
    public int SrNo { get; set; }

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PendingEmailVerification;
    public bool IsEmailVerified { get; set; } = false;

    [MaxLength(512)]
    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public ConsultantApplicationStatus? ConsultantApplicationStatus { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool EmailNotificationsEnabled  { get; set; } = true;
    public bool InAppNotificationsEnabled  { get; set; } = true;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
