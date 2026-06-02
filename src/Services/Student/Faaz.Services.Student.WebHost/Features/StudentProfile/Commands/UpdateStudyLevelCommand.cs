using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdateStudyLevelCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateStudyLevelDto PutModel { get; set; } = null!;
}

internal sealed class UpdateStudyLevelCommandHandler : IRequestHandler<UpdateStudyLevelCommand>
{
    private readonly IStudentProfileServices _profileServices;

    public UpdateStudyLevelCommandHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateStudyLevelCommand command, CancellationToken ct)
    {
        // AsNoTracking: profile won't be included in SaveChanges, so no UPDATE rowcount check.
        // Child entities are tracked independently via Remove/AddStudyDataAsync below.
        var profile = await _profileServices.GetByUserIdNoTrackingAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        // Remove ALL existing track-specific child data — we always re-insert fresh.
        // Using explicit Remove (tracked delete) rather than null-assignment to avoid
        // EF Core's orphan-deletion rowcount check on the profile UPDATE.
        if (profile.SixthFormData is not null)
            _profileServices.Remove(profile.SixthFormData);
        if (profile.UndergraduateData is not null)
            _profileServices.Remove(profile.UndergraduateData);
        if (profile.PostgraduateData is not null)
            _profileServices.Remove(profile.PostgraduateData);

        // Create the new child entity for the selected track (FK-only, not assigned to nav prop)
        switch (command.PutModel.StudyTrack)
        {
            case StudyTrack.SixthForm:
                await _profileServices.AddStudyDataAsync(new SixthFormData
                {
                    StudentProfileId = profile.Id,
                    Subjects = command.PutModel.Subjects ?? [],
                    ExamBoard = command.PutModel.ExamBoard,
                    PredictedGrades = command.PutModel.PredictedGrades,
                    School = command.PutModel.School,
                    TargetEntryYear = command.PutModel.TargetEntryYear
                }, ct);
                break;

            case StudyTrack.Undergraduate:
                await _profileServices.AddStudyDataAsync(new UndergraduateData
                {
                    StudentProfileId = profile.Id,
                    CurrentUniversity = command.PutModel.CurrentUniversity,
                    IsGapYear = command.PutModel.IsGapYear,
                    DegreeSubject = command.PutModel.DegreeSubject,
                    YearOfStudy = command.PutModel.YearOfStudy,
                    CurrentGrade = command.PutModel.CurrentGrade
                }, ct);
                break;

            case StudyTrack.Postgraduate:
                await _profileServices.AddStudyDataAsync(new PostgraduateData
                {
                    StudentProfileId = profile.Id,
                    UndergraduateUniversity = command.PutModel.UndergraduateUniversity,
                    UndergraduateDegree = command.PutModel.UndergraduateDegree,
                    UndergraduateGrade = command.PutModel.UndergraduateGrade,
                    PostgraduateStatus = command.PutModel.PostgraduateStatus,
                    ResearchInterests = command.PutModel.ResearchInterests
                }, ct);
                break;
        }

        // Compute completeness with the new track value before detaching
        profile.StudyTrack = command.PutModel.StudyTrack;
        profile.UpdateCompleteness();
        var completeness = profile.ProfileCompleteness;
        var isOnboarding = profile.IsOnboardingComplete;

        // Step 1: persist child entity changes (DELETEs + INSERT)
        // Profile was loaded AsNoTracking so it is NOT in the change tracker —
        // SaveChanges only processes the child entity deletes/inserts.
        await _profileServices.SaveChangesAsync(ct);

        // Step 2: update profile fields directly via ExecuteUpdate (bypasses change tracking)
        await _profileServices.ExecuteUpdateStudyTrackAsync(
            profile.Id, command.PutModel.StudyTrack, completeness, isOnboarding, ct);
    }
}
