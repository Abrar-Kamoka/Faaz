using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Student.WebHost.Consumers;

public class StudentRegisteredConsumer : IConsumer<StudentRegisteredEvent>
{
    private readonly IStudentProfileServices _profileServices;
    private readonly ILogger<StudentRegisteredConsumer> _logger;

    public StudentRegisteredConsumer(
        IStudentProfileServices profileServices,
        ILogger<StudentRegisteredConsumer> logger)
    {
        _profileServices = profileServices;
        _logger          = logger;
    }

    public async Task Consume(ConsumeContext<StudentRegisteredEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        if (await _profileServices.ExistsForUserAsync(msg.UserId, ct))
        {
            _logger.LogInformation("Student profile already exists for {UserId} — skipping stub creation", msg.UserId);
            return;
        }

        var profile = new StudentProfile
        {
            UserId    = msg.UserId,
            Email     = msg.Email,
            FirstName = msg.FirstName,
            LastName  = msg.LastName
        };

        await _profileServices.AddAsync(profile, ct);
        await _profileServices.SaveChangesAsync(ct);

        _logger.LogInformation("Student profile stub created for {UserId}", msg.UserId);
    }
}
