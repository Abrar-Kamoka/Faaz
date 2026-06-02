namespace Faaz.SharedKernel;

/// <summary>Enums shared across Identity, Consultant, and Student services.</summary>
public class SharedEnums
{
    public enum ConsultantApplicationStatus
    {
        Submitted        = 0,
        Invited          = 1,  // Admin sent invite
        SettingUpProfile = 2,  // Email verified, filling profile
        PendingRevision  = 3,
        Active           = 4,  // Profile complete, auto-activated
        Rejected         = 5,
        Suspended        = 6
    }

    public enum ApplicationDocumentType
    {
        CV                        = 1,
        ProfessionalCertification = 2,
        DegreeOrDiploma           = 3,
        GovernmentIssuedId        = 4,
        ProofOfAddress            = 5,
        ProfessionalMembership    = 6,
        ReferenceLetter           = 7,
        OtherSupportingDocument   = 8
    }

    public enum HighestQualification
    {
        Undergraduate             = 1,
        Postgraduate              = 2,
        Doctorate                 = 3,
        ProfessionalQualification = 4,
        Other                     = 5
    }

    public enum ConsultationMode
    {
        Online   = 1,
        InPerson = 2,
        Both     = 3
    }
}
