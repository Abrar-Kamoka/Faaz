using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateBioAndVideoCommandValidator : AbstractValidator<UpdateBioAndVideoCommand>
{
    public UpdateBioAndVideoCommandValidator()
    {
        RuleFor(x => x.PutModel.WrittenBio).NotEmpty().MinimumLength(50).MaximumLength(2000);
        RuleFor(x => x.PutModel.IntroVideoUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.PutModel.IntroVideoUrl))
            .WithMessage("IntroVideoUrl must be a valid URL.");
    }
}
