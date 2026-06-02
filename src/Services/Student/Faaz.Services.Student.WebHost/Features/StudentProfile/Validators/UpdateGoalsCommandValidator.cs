using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Validators;

internal sealed class UpdateGoalsCommandValidator : AbstractValidator<UpdateGoalsCommand>
{
    public UpdateGoalsCommandValidator()
    {
        RuleFor(x => x.PutModel.TargetStudyLevel).IsInEnum();
        RuleFor(x => x.PutModel.TargetSubjects).Must(s => s.Length <= 10).WithMessage("Maximum 10 target subjects.");
        RuleFor(x => x.PutModel.TargetUniversities).Must(u => u.Length <= 10).WithMessage("Maximum 10 target universities.");
        RuleFor(x => x.PutModel.HelpTypes).Must(h => h != 0).WithMessage("At least one help type must be selected.");
    }
}
