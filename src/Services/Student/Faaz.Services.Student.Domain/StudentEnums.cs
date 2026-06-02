namespace Faaz.Services.Student.Domain;

public class StudentEnums
{
    public enum StudyTrack
    {
        SixthForm = 1,
        Undergraduate = 2,
        Postgraduate = 3
    }

    public enum StudyLevel
    {
        ALevel = 1,
        Undergraduate = 2,
        Postgraduate = 3,
        Phd = 4
    }

    [Flags]
    public enum HelpType
    {
        PersonalStatement = 1,
        InterviewPrep = 2,
        Ucas = 4,
        Sop = 8,
        Scholarships = 16,
        Visa = 32,
        GeneralGuidance = 64
    }
}
