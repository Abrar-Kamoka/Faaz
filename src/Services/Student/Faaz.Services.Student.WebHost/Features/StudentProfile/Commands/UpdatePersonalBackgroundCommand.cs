using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;

public class UpdatePersonalBackgroundCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdatePersonalBackgroundDto PutModel { get; set; } = null!;
}

internal sealed class UpdatePersonalBackgroundCommandHandler : IRequestHandler<UpdatePersonalBackgroundCommand>
{
    private readonly IStudentProfileServices _profileServices;

    public UpdatePersonalBackgroundCommandHandler(IStudentProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdatePersonalBackgroundCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("StudentProfile", command.UserId);

        profile.FirstName = command.PutModel.FirstName;
        profile.LastName = command.PutModel.LastName;
        profile.DateOfBirth = command.PutModel.DateOfBirth;
        profile.CountryOfCitizenship = command.PutModel.CountryOfCitizenship;
        profile.CountryOfResidence = command.PutModel.CountryOfResidence;
        profile.Ethnicity = command.PutModel.Ethnicity;
        profile.FirstLanguage = command.PutModel.FirstLanguage;
        profile.AdditionalLanguages = command.PutModel.AdditionalLanguages;
        profile.UpdateCompleteness();
        await _profileServices.SaveChangesAsync(ct);
    }
}
