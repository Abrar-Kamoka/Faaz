using Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Validators;

internal sealed class RejectApplicationCommandValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PostModel.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PostModel.Notes).NotEmpty().MaximumLength(500);
    }
}
