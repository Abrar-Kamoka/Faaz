using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Administration.WebHost.DevConsumers;

internal sealed class ConsultantSuspendedDevConsumer(ILogger<ConsultantSuspendedDevConsumer> logger)
    : IConsumer<ConsultantSuspendedByAdminEvent>
{
    public Task Consume(ConsumeContext<ConsultantSuspendedByAdminEvent> ctx)
    {
        logger.LogInformation(
            "[DEV] ConsultantSuspendedByAdmin: consultant={Id} by admin={AdminId} reason='{Reason}'",
            ctx.Message.ConsultantUserId, ctx.Message.AdminId, ctx.Message.Reason);
        return Task.CompletedTask;
    }
}

internal sealed class ConsultantRestoredDevConsumer(ILogger<ConsultantRestoredDevConsumer> logger)
    : IConsumer<ConsultantRestoredByAdminEvent>
{
    public Task Consume(ConsumeContext<ConsultantRestoredByAdminEvent> ctx)
    {
        logger.LogInformation(
            "[DEV] ConsultantRestoredByAdmin: consultant={Id} by admin={AdminId}",
            ctx.Message.ConsultantUserId, ctx.Message.AdminId);
        return Task.CompletedTask;
    }
}

internal sealed class UserDeactivatedDevConsumer(ILogger<UserDeactivatedDevConsumer> logger)
    : IConsumer<UserDeactivatedByAdminEvent>
{
    public Task Consume(ConsumeContext<UserDeactivatedByAdminEvent> ctx)
    {
        logger.LogInformation(
            "[DEV] UserDeactivatedByAdmin: user={Id} by admin={AdminId} reason='{Reason}'",
            ctx.Message.UserId, ctx.Message.AdminId, ctx.Message.Reason);
        return Task.CompletedTask;
    }
}

internal sealed class UserReactivatedDevConsumer(ILogger<UserReactivatedDevConsumer> logger)
    : IConsumer<UserReactivatedByAdminEvent>
{
    public Task Consume(ConsumeContext<UserReactivatedByAdminEvent> ctx)
    {
        logger.LogInformation(
            "[DEV] UserReactivatedByAdmin: user={Id} by admin={AdminId}",
            ctx.Message.UserId, ctx.Message.AdminId);
        return Task.CompletedTask;
    }
}
