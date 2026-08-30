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
        CV                             = 1,
        ProfessionalCertification      = 2,
        DegreeOrDiploma                = 3,
        GovernmentIssuedId             = 4,
        ProofOfAddress                 = 5,
        ProfessionalMembership         = 6,
        ReferenceLetter                = 7,
        OtherSupportingDocument        = 8,
        AffiliationOrAppointmentLetter = 9
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

    // RQF-anchored study level, shared by Consultant (levels advised on) and Student (target level) —
    // was two independently-duplicated local enums (ConsultantEnums.StudyLevel / StudentEnums.StudyLevel).
    // PhD/Doctorate lives under PostgraduateResearch rather than as its own value.
    public enum StudyLevel
    {
        SixthForm            = 1, // A-Level / IB / BTEC — RQF Level 3
        Foundation           = 2, // Foundation year/pathway — bridges to RQF Level 4
        Undergraduate        = 3, // Bachelor's / HNC / HND — RQF Levels 4-6
        PostgraduateTaught   = 4, // Masters / PGDip / PGCert — RQF Level 7
        PostgraduateResearch = 5  // MPhil / PhD / Doctorate — RQF Levels 7-8
    }
}
