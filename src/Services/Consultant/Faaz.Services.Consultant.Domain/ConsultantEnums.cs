namespace Faaz.Services.Consultant.Domain;

/// <summary>Enums exclusive to the Consultant service. Cross-service enums live in Faaz.SharedKernel.SharedEnums.</summary>
public class ConsultantEnums
{
    public enum CallPreference
    {
        AudioOnly = 1,
        VideoOnly = 2,
        Both      = 3
    }

    public enum StudyLevel
    {
        ALevel        = 1,
        Undergraduate = 2,
        Postgraduate  = 3,
        Phd           = 4
    }

    public enum ServiceType
    {
        PersonalStatement = 1,
        Ucas              = 2,
        InterviewPrep     = 3,
        Sop               = 4,
        Scholarships      = 5,
        Visa              = 6,
        GeneralGuidance   = 7
    }
}
