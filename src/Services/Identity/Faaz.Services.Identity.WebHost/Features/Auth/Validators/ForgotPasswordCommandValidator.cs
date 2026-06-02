using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Validators;

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.PostModel.Email).NotEmpty().EmailAddress();
    }
}
