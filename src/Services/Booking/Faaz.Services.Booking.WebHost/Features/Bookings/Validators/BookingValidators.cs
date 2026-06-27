using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.Services.Booking.WebHost.Features.Reviews.DTOs;
using FluentValidation;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Validators;

public class CreateBookingValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.ConsultantProfileId).NotEmpty();
        RuleFor(x => x.ScheduledStartUtc)
            .GreaterThan(DateTime.UtcNow.AddMinutes(30))
            .WithMessage("Booking must be at least 30 minutes in the future.");
        RuleFor(x => x.CallType).InclusiveBetween(1, 3);
    }
}

public class SubmitRefundAppealValidator : AbstractValidator<SubmitRefundAppealDto>
{
    public SubmitRefundAppealValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public class SubmitReviewValidator : AbstractValidator<SubmitReviewDto>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.ReviewText).MaximumLength(2000);
    }
}
