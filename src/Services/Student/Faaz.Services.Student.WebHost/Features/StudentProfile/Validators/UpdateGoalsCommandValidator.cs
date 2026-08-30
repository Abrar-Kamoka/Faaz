using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Validators;

internal sealed class UpdateGoalsCommandValidator : AbstractValidator<UpdateGoalsCommand>
{
    public UpdateGoalsCommandValidator()
    {
        RuleFor(x => x.PutModel.TargetStudyLevel).IsInEnum();
        RuleFor(x => x.PutModel.TargetSubjectIds).Must(s => s.Length <= 10).WithMessage("Maximum 10 target subjects.");
        RuleFor(x => x.PutModel.TargetUniversityIds).Must(u => u.Length <= 10).WithMessage("Maximum 10 target universities.");
        RuleFor(x => x.PutModel.TargetProgrammeIds).Must(p => p.Length <= 10).WithMessage("Maximum 10 target programmes.");
        RuleFor(x => x.PutModel.HelpServiceIds).NotEmpty().WithMessage("At least one help service must be selected.");
    }
}
