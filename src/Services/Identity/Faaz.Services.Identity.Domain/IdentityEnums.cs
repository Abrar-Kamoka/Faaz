namespace Faaz.Services.Identity.Domain;

/// <summary>Enums exclusive to the Identity service. Cross-service enums live in Faaz.SharedKernel.SharedEnums.</summary>
public class IdentityEnums
{
    public enum UserRole   { Student = 1, Consultant = 2, SuperAdmin = 3 }

    public enum UserStatus
    {
        Active                   = 1,
        Suspended                = 2,
        PendingEmailVerification = 3,
        Rejected                 = 4
    }
}
