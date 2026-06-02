using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Validators;

internal sealed class RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>
{
    public RegisterStudentCommandValidator()
    {
        RuleFor(x => x.PostModel.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PostModel.Password).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.PostModel.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostModel.LastName).NotEmpty().MaximumLength(100);
    }
}
