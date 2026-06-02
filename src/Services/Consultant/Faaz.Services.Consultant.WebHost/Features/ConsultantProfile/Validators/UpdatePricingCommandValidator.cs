using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdatePricingCommandValidator : AbstractValidator<UpdatePricingCommand>
{
    public UpdatePricingCommandValidator()
    {
        RuleFor(x => x.PutModel.SessionTypes).NotEmpty().WithMessage("At least one session type is required.");
        RuleForEach(x => x.PutModel.SessionTypes).ChildRules(session =>
        {
            session.RuleFor(s => s.Name).NotEmpty().MaximumLength(100);
            session.RuleFor(s => s.DurationMinutes).InclusiveBetween(15, 480);
            session.RuleFor(s => s.PriceGbp).GreaterThanOrEqualTo(0).LessThan(10000);
        });
    }
}
