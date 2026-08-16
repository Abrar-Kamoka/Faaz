using System.Text.RegularExpressions;
using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using FluentValidation;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Validators;

internal sealed partial class RegisterConsultantCommandValidator : AbstractValidator<RegisterConsultantCommand>
{
    // Mirrors the frontend's E.164-shaped check (lib/phone-codes.ts PHONE_REGEX).
    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex PhoneNumberRegex();

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
        RuleFor(x => x.PostModel.PhoneNumber).NotEmpty().MaximumLength(30)
            .Must(p => PhoneNumberRegex().IsMatch(p))
            .WithMessage("Phone number must include a country code, e.g. +447700900000.");
        RuleFor(x => x.PostModel.CurrentRole).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostModel.Institution).MaximumLength(200).When(x => x.PostModel.Institution is not null);
        RuleFor(x => x.PostModel.ExpertiseArea).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PostModel.YearsOfExperience).InclusiveBetween(1, 50);
        RuleFor(x => x.PostModel.LinkedInProfileUrl).MaximumLength(500).When(x => x.PostModel.LinkedInProfileUrl is not null);
        RuleFor(x => x.PostModel.PersonalStatement).MaximumLength(2000).When(x => x.PostModel.PersonalStatement is not null);
        RuleFor(x => x.PostModel.ReferralSource).MaximumLength(100).When(x => x.PostModel.ReferralSource is not null);

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
