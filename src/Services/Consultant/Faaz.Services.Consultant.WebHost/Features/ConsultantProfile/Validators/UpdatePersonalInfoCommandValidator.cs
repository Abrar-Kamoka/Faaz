using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdatePersonalInfoCommandValidator : AbstractValidator<UpdatePersonalInfoCommand>
{
    public UpdatePersonalInfoCommandValidator()
    {
        RuleFor(x => x.PutModel.FullLegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PutModel.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PutModel.CurrentRole).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PutModel.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PutModel.YearsOfExperience).InclusiveBetween(0, 60);
        RuleFor(x => x.PutModel.LinkedInUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("LinkedInUrl must be a valid URL.");
    }
}
