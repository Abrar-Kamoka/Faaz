using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateCallPreferencesCommandValidator : AbstractValidator<UpdateCallPreferencesCommand>
{
    public UpdateCallPreferencesCommandValidator()
    {
        RuleFor(x => x.PutModel.CallPreference).InclusiveBetween(1, 3)
            .WithMessage("CallPreference must be a valid value.");
        RuleFor(x => x.PutModel.MinBookingNoticeHours).InclusiveBetween(0, 168)
            .WithMessage("Minimum notice must be between 0 and 168 hours (1 week).");
        RuleFor(x => x.PutModel.MaxAdvanceBookingDays).InclusiveBetween(1, 365)
            .WithMessage("Max advance booking must be between 1 and 365 days.");
    }
}
