using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantSuspendedByAdminConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<ConsultantSuspendedByAdminConsumer> logger) : IConsumer<ConsultantSuspendedByAdminEvent>
{
    public async Task Consume(ConsumeContext<ConsultantSuspendedByAdminEvent> ctx)
    {
        var msg = ctx.Message;
        var body = $"Your consultant account has been suspended. Reason: {msg.Reason}. Please contact support for assistance.";

        await hub.Clients.Group($"user-{msg.ConsultantUserId}").SendAsync(
            "AccountSuspended", new { Type = "AccountSuspended", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ConsultantSuspendedByAdminEvent),
            Subject = "Account suspended",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("ConsultantSuspended notification sent to {Id}", msg.ConsultantUserId);
    }
}

public class ConsultantRestoredByAdminConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<ConsultantRestoredByAdminConsumer> logger) : IConsumer<ConsultantRestoredByAdminEvent>
{
    public async Task Consume(ConsumeContext<ConsultantRestoredByAdminEvent> ctx)
    {
        var msg = ctx.Message;
        var body = "Your consultant account has been restored and is now active.";

        await hub.Clients.Group($"user-{msg.ConsultantUserId}").SendAsync(
            "AccountRestored", new { Type = "AccountRestored", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ConsultantRestoredByAdminEvent),
            Subject = "Account restored",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("ConsultantRestored notification sent to {Id}", msg.ConsultantUserId);
    }
}

public class UserDeactivatedByAdminConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<UserDeactivatedByAdminConsumer> logger) : IConsumer<UserDeactivatedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserDeactivatedByAdminEvent> ctx)
    {
        var msg = ctx.Message;
        var body = $"Your account has been deactivated. Reason: {msg.Reason}. Contact support if you believe this is a mistake.";

        await hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "AccountDeactivated", new { Type = "AccountDeactivated", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(UserDeactivatedByAdminEvent),
            Subject = "Account deactivated",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("UserDeactivated notification sent to {Id}", msg.UserId);
    }
}

public class ReferenceRequestApprovedConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<ReferenceRequestApprovedConsumer> logger) : IConsumer<ReferenceRequestApprovedEvent>
{
    public async Task Consume(ConsumeContext<ReferenceRequestApprovedEvent> ctx)
    {
        var msg = ctx.Message;
        var body = $"Good news — your request for \"{msg.ProposedName}\" was approved. Our team is finishing up " +
                   "adding it to the catalog, so it may take a little while to appear in search.";

        await hub.Clients.Group($"user-{msg.RequestedByUserId}").SendAsync(
            "ReferenceRequestApproved", new { Type = "ReferenceRequestApproved", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.RequestedByUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ReferenceRequestApprovedEvent),
            Subject = $"Your \"{msg.ProposedName}\" request was approved",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("ReferenceRequestApproved notification sent to {Id}", msg.RequestedByUserId);
    }
}

public class ReferenceRequestRejectedConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<ReferenceRequestRejectedConsumer> logger) : IConsumer<ReferenceRequestRejectedEvent>
{
    public async Task Consume(ConsumeContext<ReferenceRequestRejectedEvent> ctx)
    {
        var msg = ctx.Message;
        var body = $"Your request for \"{msg.ProposedName}\" wasn't approved." +
                   (string.IsNullOrWhiteSpace(msg.ReviewNotes) ? "" : $" Note from our team: {msg.ReviewNotes}");

        await hub.Clients.Group($"user-{msg.RequestedByUserId}").SendAsync(
            "ReferenceRequestRejected", new { Type = "ReferenceRequestRejected", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.RequestedByUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ReferenceRequestRejectedEvent),
            Subject = $"Your \"{msg.ProposedName}\" request wasn't approved",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("ReferenceRequestRejected notification sent to {Id}", msg.RequestedByUserId);
    }
}

public class UserReactivatedByAdminConsumer(
    INotificationLogServices logServices,
    IHubContext<NotificationHub> hub,
    ILogger<UserReactivatedByAdminConsumer> logger) : IConsumer<UserReactivatedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserReactivatedByAdminEvent> ctx)
    {
        var msg = ctx.Message;
        var body = "Your account has been reactivated. Welcome back!";

        await hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "AccountReactivated", new { Type = "AccountReactivated", Message = body }, ctx.CancellationToken);

        await logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(UserReactivatedByAdminEvent),
            Subject = "Account reactivated",
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ctx.CancellationToken);
        await logServices.SaveChangesAsync(ctx.CancellationToken);
        logger.LogInformation("UserReactivated notification sent to {Id}", msg.UserId);
    }
}
