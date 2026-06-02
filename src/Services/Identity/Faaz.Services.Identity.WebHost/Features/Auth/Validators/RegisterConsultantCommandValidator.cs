using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Validators;

internal sealed class RegisterConsultantCommandValidator : AbstractValidator<RegisterConsultantCommand>
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/png",
        "image/jpeg",
        "image/gif"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public RegisterConsultantCommandValidator()
    {
        RuleFor(x => x.PostModel.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.PostModel.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostModel.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostModel.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PostModel.CurrentRole).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostModel.ExpertiseArea).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostModel.YearsOfExperience).InclusiveBetween(1, 50);
        RuleFor(x => x.PostModel.LinkedInProfileUrl).MaximumLength(500).When(x => x.PostModel.LinkedInProfileUrl is not null);
        RuleFor(x => x.PostModel.PersonalStatement).MaximumLength(2000).When(x => x.PostModel.PersonalStatement is not null);

        When(x => x.PostModel.Files is { Count: > 0 }, () =>
        {
            RuleFor(x => x.PostModel.Files!.Count)
                .LessThanOrEqualTo(10)
                .WithMessage("A maximum of 10 documents may be uploaded.");

            RuleForEach(x => x.PostModel.Files).ChildRules(file =>
            {
                file.RuleFor(f => f.Length)
                    .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage("Each file must be 10 MB or smaller.");

                file.RuleFor(f => f.ContentType)
                    .Must(ct => AllowedMimeTypes.Contains(ct))
                    .WithMessage("Only PDF, Word (.doc/.docx), PNG, JPG, and GIF files are accepted.");
            });
        });
    }
}
