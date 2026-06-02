using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Validators;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.PostModel.Token).NotEmpty();
        RuleFor(x => x.PostModel.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.PostModel.ConfirmPassword)
            .Equal(x => x.PostModel.NewPassword).WithMessage("Passwords do not match.");
    }
}
