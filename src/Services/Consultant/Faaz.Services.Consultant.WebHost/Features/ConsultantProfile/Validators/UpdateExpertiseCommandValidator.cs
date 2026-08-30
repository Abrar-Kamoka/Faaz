using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using FluentValidation;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Validators;

public class UpdateExpertiseCommandValidator : AbstractValidator<UpdateExpertiseCommand>
{
    public UpdateExpertiseCommandValidator()
    {
        RuleFor(x => x.PutModel.StudyLevelsOffered).NotEmpty().WithMessage("At least one study level is required.");
        RuleFor(x => x.PutModel.ServiceIds).NotEmpty().WithMessage("At least one service is required.");
        RuleFor(x => x.PutModel.SubjectIds).NotEmpty().WithMessage("At least one subject is required.");

        RuleForEach(x => x.PutModel.StudyLevelsOffered).IsInEnum().WithMessage("Invalid StudyLevel value.");
        RuleFor(x => x.PutModel.SubjectIds).Must(ids => ids.Length <= 20).WithMessage("At most 20 subjects.");
        RuleFor(x => x.PutModel.UniversityIds).Must(ids => ids.Length <= 20).WithMessage("At most 20 universities.");
        RuleFor(x => x.PutModel.ServiceIds).Must(ids => ids.Length <= 20).WithMessage("At most 20 services.");
    }
}
