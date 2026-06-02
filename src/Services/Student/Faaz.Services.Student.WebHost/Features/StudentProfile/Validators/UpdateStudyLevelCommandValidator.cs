using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Validators;

internal sealed class UpdateStudyLevelCommandValidator : AbstractValidator<UpdateStudyLevelCommand>
{
    public UpdateStudyLevelCommandValidator()
    {
        RuleFor(x => x.PutModel.StudyTrack).IsInEnum();
    }
}
