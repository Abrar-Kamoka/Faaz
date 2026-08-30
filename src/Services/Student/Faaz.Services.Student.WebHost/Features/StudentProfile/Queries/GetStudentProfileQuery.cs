using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

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
            // References into Administration's real catalog — the client already holds that catalog
            // (fetched via /api/v1/reference/*) to join these ids against names for display.
            TargetSubjectIds = p.TargetSubjects.Select(s => s.SubjectId).ToArray(),
            TargetUniversityIds = p.TargetUniversities.Select(u => u.UniversityId).ToArray(),
            TargetProgrammeIds = p.TargetProgrammes.Select(pr => pr.ProgrammeId).ToArray(),
            HelpServiceIds = p.HelpServices.Select(s => s.ServiceId).ToArray(),
            ProfilePhotoUrl = p.ProfilePhotoUrl,
            Bio = p.Bio,
            ProfileCompleteness = p.ProfileCompleteness,
            IsOnboardingComplete = p.IsOnboardingComplete
        };
    }
}
