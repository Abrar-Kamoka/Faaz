using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Validators;

internal sealed class UpdateProfileBioCommandValidator : AbstractValidator<UpdateProfileBioCommand>
{
    public UpdateProfileBioCommandValidator()
    {
        RuleFor(x => x.PutModel.Bio).MaximumLength(500);
    }
}
