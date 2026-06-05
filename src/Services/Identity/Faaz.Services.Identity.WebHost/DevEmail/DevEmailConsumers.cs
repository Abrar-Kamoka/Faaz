using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.DevEmail;

// In-memory MassTransit dev consumers — receive events published by Identity and deliver
// them to IEmailService (DevSmtpEmailService: tries MailHog, falls back to console log).

internal sealed class StudentRegisteredDevConsumer(IEmailService email) : IConsumer<StudentRegisteredEvent>
{
    public Task Consume(ConsumeContext<StudentRegisteredEvent> ctx) =>
        email.SendEmailVerificationAsync(ctx.Message.Email, ctx.Message.FirstName, ctx.Message.VerificationToken, ctx.CancellationToken);
}

internal sealed class SendVerificationEmailDevConsumer(IEmailService email) : IConsumer<SendVerificationEmailEvent>
{
    public Task Consume(ConsumeContext<SendVerificationEmailEvent> ctx) =>
        email.SendEmailVerificationAsync(ctx.Message.To, ctx.Message.FirstName, ctx.Message.VerificationToken, ctx.CancellationToken);
}

internal sealed class SendPasswordResetEmailDevConsumer(IEmailService email) : IConsumer<SendPasswordResetEmailEvent>
{
    public Task Consume(ConsumeContext<SendPasswordResetEmailEvent> ctx) =>
        email.SendPasswordResetAsync(ctx.Message.To, ctx.Message.FirstName, ctx.Message.ResetToken, ctx.CancellationToken);
}

internal sealed class ConsultantApprovedDevConsumer(IEmailService email) : IConsumer<ConsultantApprovedEvent>
{
    public Task Consume(ConsumeContext<ConsultantApprovedEvent> ctx) =>
        email.SendConsultantApprovalAsync(ctx.Message.Email, ctx.Message.FirstName, ctx.CancellationToken);
}

internal sealed class ConsultantRejectedDevConsumer(IEmailService email) : IConsumer<ConsultantRejectedEvent>
{
    public Task Consume(ConsumeContext<ConsultantRejectedEvent> ctx) =>
        email.SendConsultantRejectionAsync(ctx.Message.Email, "Consultant", ctx.Message.Reason, ctx.CancellationToken);
}

internal sealed class ConsultantRevisionRequestedDevConsumer(IEmailService email) : IConsumer<ConsultantRevisionRequestedEvent>
{
    public Task Consume(ConsumeContext<ConsultantRevisionRequestedEvent> ctx) =>
        email.SendConsultantRevisionRequestAsync(ctx.Message.Email, "Consultant", ctx.Message.Notes, ctx.CancellationToken);
}

// Mirrors what ConsultantEmailVerifiedConsumer does in the Consultant service via RabbitMQ in production.
internal sealed class ConsultantEmailVerifiedDevConsumer(
    IConsultantServiceClient consultantClient,
    ILogger<ConsultantEmailVerifiedDevConsumer> logger) : IConsumer<ConsultantEmailVerifiedEvent>
{
    public async Task Consume(ConsumeContext<ConsultantEmailVerifiedEvent> ctx)
    {
        try
        {
            await consultantClient.NotifyEmailVerifiedAsync(ctx.Message.UserId, ctx.CancellationToken);
            logger.LogInformation("[DEV] Consultant profile stub created for {UserId}", ctx.Message.UserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[DEV] Could not create consultant profile stub — is the Consultant service running? {Message}", ex.Message);
        }
    }
}

// Mirrors what StudentRegisteredConsumer does in the Student service via RabbitMQ in production.
internal sealed class StudentProfileCreatorDevConsumer(
    IStudentServiceClient studentClient,
    ILogger<StudentProfileCreatorDevConsumer> logger) : IConsumer<StudentRegisteredEvent>
{
    public async Task Consume(ConsumeContext<StudentRegisteredEvent> ctx)
    {
        try
        {
            var m = ctx.Message;
            await studentClient.CreateProfileStubAsync(m.UserId, m.Email, m.FirstName, m.LastName, ctx.CancellationToken);
            logger.LogInformation("[DEV] Student profile stub created for {UserId}", m.UserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[DEV] Could not create student profile stub — is the Student service running? {Message}", ex.Message);
        }
    }
}
