using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(x => x.PutModel.WeeklySlots).NotNull();
        RuleForEach(x => x.PutModel.WeeklySlots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.DayOfWeek).InclusiveBetween(0, 6);
            slot.RuleFor(s => s.StartTime).LessThan(s => s.EndTime)
                .WithMessage("StartTime must be before EndTime.");
        });
        RuleForEach(x => x.PutModel.BlockedDates).ChildRules(blocked =>
        {
            blocked.RuleFor(b => b.Date).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Blocked date cannot be in the past.");
        });
    }
}
