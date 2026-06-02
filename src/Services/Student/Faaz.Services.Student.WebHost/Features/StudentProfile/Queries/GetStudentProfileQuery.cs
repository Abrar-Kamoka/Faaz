using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Queries;

public class GetStudentProfileQuery : IRequest<StudentProfileDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetStudentProfileQueryHandler : IRequestHandler<GetStudentProfileQuery, StudentProfileDto>
{
    private readonly IStudentProfileServices _profileServices;

    public GetStudentProfileQueryHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task<StudentProfileDto> Handle(GetStudentProfileQuery query, CancellationToken ct)
    {
        var p = await _profileServices.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", query.UserId);

        var helpTypeFlags = Enum.GetValues<HelpType>()
            .Where(flag => p.HelpTypes.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToArray();

        return new StudentProfileDto
        {
            UserId = p.UserId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            CountryOfCitizenship = p.CountryOfCitizenship,
            CountryOfResidence = p.CountryOfResidence,
            Ethnicity = p.Ethnicity,
            FirstLanguage = p.FirstLanguage,
            AdditionalLanguages = p.AdditionalLanguages,
            StudyTrack = p.StudyTrack?.ToString(),
            SixthFormData = p.SixthFormData is null ? null : new SixthFormDataDto
            {
                Subjects = p.SixthFormData.Subjects,
                ExamBoard = p.SixthFormData.ExamBoard,
                PredictedGrades = p.SixthFormData.PredictedGrades,
                School = p.SixthFormData.School,
                TargetEntryYear = p.SixthFormData.TargetEntryYear
            },
            UndergraduateData = p.UndergraduateData is null ? null : new UndergraduateDataDto
            {
                CurrentUniversity = p.UndergraduateData.CurrentUniversity,
                IsGapYear = p.UndergraduateData.IsGapYear,
                DegreeSubject = p.UndergraduateData.DegreeSubject,
                YearOfStudy = p.UndergraduateData.YearOfStudy,
                CurrentGrade = p.UndergraduateData.CurrentGrade
            },
            PostgraduateData = p.PostgraduateData is null ? null : new PostgraduateDataDto
            {
                UndergraduateUniversity = p.PostgraduateData.UndergraduateUniversity,
                UndergraduateDegree = p.PostgraduateData.UndergraduateDegree,
                UndergraduateGrade = p.PostgraduateData.UndergraduateGrade,
                PostgraduateStatus = p.PostgraduateData.PostgraduateStatus,
                ResearchInterests = p.PostgraduateData.ResearchInterests
            },
            TargetStudyLevel = p.TargetStudyLevel?.ToString(),
            TargetSubjects = p.TargetSubjects,
            TargetUniversities = p.TargetUniversities,
            HelpTypes = helpTypeFlags,
            ProfilePhotoUrl = p.ProfilePhotoUrl,
            Bio = p.Bio,
            ProfileCompleteness = p.ProfileCompleteness,
            IsOnboardingComplete = p.IsOnboardingComplete
        };
    }
}
