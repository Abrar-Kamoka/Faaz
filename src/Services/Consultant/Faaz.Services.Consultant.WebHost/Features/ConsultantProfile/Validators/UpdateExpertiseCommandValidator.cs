using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateExpertiseCommandValidator : AbstractValidator<UpdateExpertiseCommand>
{
    public UpdateExpertiseCommandValidator()
    {
        RuleFor(x => x.PutModel.StudyLevelsOffered).NotEmpty().WithMessage("At least one study level is required.");
        RuleFor(x => x.PutModel.ServicesOffered).NotEmpty().WithMessage("At least one service type is required.");
        RuleFor(x => x.PutModel.SubjectAreas).NotEmpty().WithMessage("At least one subject area is required.");

        RuleForEach(x => x.PutModel.StudyLevelsOffered).IsInEnum().WithMessage("Invalid StudyLevel value.");
        RuleForEach(x => x.PutModel.ServicesOffered).IsInEnum().WithMessage("Invalid ServiceType value.");
        RuleForEach(x => x.PutModel.SubjectAreas).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.PutModel.SpecialisedUniversities).NotEmpty().MaximumLength(200);
    }
}
