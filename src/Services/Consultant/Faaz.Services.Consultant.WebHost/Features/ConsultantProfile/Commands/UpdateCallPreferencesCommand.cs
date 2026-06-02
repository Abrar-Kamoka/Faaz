using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;

public class UpdateCallPreferencesCommand : IRequest
{
    public Guid UserId { get; set; }
    public UpdateCallPreferencesDto PutModel { get; set; } = null!;
}

internal sealed class UpdateCallPreferencesCommandHandler : IRequestHandler<UpdateCallPreferencesCommand>
{
    private readonly IConsultantProfileServices _profileServices;

    public UpdateCallPreferencesCommandHandler(IConsultantProfileServices profileServices)
    {
        _profileServices = profileServices;
    }

    public async Task Handle(UpdateCallPreferencesCommand command, CancellationToken ct)
    {
        var profile = await _profileServices.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("ConsultantProfile", command.UserId);

        profile.CallPreference = (CallPreference)command.PutModel.CallPreference;
        profile.MinBookingNoticeHours = command.PutModel.MinBookingNoticeHours;
        profile.MaxAdvanceBookingDays = command.PutModel.MaxAdvanceBookingDays;

        await _profileServices.TryAutoActivateAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);
    }
}
