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

    // StudyLevel and ServiceType used to live here — replaced by Faaz.SharedKernel.SharedEnums.StudyLevel
    // and the Administration service's admin-editable Service catalog (see ConsultantProfileService),
    // so a consultant's expertise references the same real, curated data students see in the wizard.
}
