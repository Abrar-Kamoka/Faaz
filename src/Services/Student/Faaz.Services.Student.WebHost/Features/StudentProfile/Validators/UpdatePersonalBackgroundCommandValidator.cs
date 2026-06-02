using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Validators;

internal sealed class UpdatePersonalBackgroundCommandValidator : AbstractValidator<UpdatePersonalBackgroundCommand>
{
    public UpdatePersonalBackgroundCommandValidator()
    {
        RuleFor(x => x.PutModel.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PutModel.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PutModel.FirstLanguage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PutModel.AdditionalLanguages).Must(l => l.Length <= 10).WithMessage("Maximum 10 additional languages.");
        RuleFor(x => x.PutModel.DateOfBirth).Must(d => !d.HasValue || d.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.");
    }
}
