using Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Validators;

internal sealed class ApproveApplicationCommandValidator : AbstractValidator<ApproveApplicationCommand>
{
    public ApproveApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PostModel.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PostModel.Notes).MaximumLength(2000);
    }
}
