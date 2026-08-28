using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(x => x.PutModel.TimeZoneId).NotEmpty()
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
            .WithMessage("TimeZoneId must be a valid IANA timezone identifier (e.g. 'Europe/London').");
        RuleFor(x => x.PutModel.MinBookingNoticeHours).InclusiveBetween(0, 168)
            .When(x => x.PutModel.MinBookingNoticeHours.HasValue)
            .WithMessage("Minimum notice must be between 0 and 168 hours (1 week).");
        RuleFor(x => x.PutModel.MaxAdvanceBookingDays).InclusiveBetween(1, 365)
            .When(x => x.PutModel.MaxAdvanceBookingDays.HasValue)
            .WithMessage("Max advance booking must be between 1 and 365 days.");
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
