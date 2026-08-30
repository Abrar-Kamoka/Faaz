namespace Faaz.Services.Student.Domain;

public class StudentEnums
{
    public enum StudyTrack
    {
        SixthForm = 1,
        Undergraduate = 2,
        Postgraduate = 3
    }

    // StudyLevel and HelpType used to live here — replaced by Faaz.SharedKernel.SharedEnums.StudyLevel
    // and the Administration service's admin-editable Service catalog (see StudentProfileHelpService),
    // the same shared vocabulary Consultant's ConsultantProfileService uses.
}
