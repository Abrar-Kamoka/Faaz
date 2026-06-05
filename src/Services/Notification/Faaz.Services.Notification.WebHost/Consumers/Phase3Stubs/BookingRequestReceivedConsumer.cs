using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers.Phase3Stubs;

public class BookingRequestReceivedConsumer : IConsumer<BookingRequestReceivedEvent>
{
    private readonly ILogger<BookingRequestReceivedConsumer> _logger;
    public BookingRequestReceivedConsumer(ILogger<BookingRequestReceivedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<BookingRequestReceivedEvent> context) { _logger.LogInformation("[Phase 3 stub] BookingRequestReceived. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly ILogger<BookingConfirmedConsumer> _logger;
    public BookingConfirmedConsumer(ILogger<BookingConfirmedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<BookingConfirmedEvent> context) { _logger.LogInformation("[Phase 3 stub] BookingConfirmed. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class BookingCancelledConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly ILogger<BookingCancelledConsumer> _logger;
    public BookingCancelledConsumer(ILogger<BookingCancelledConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<BookingCancelledEvent> context) { _logger.LogInformation("[Phase 3 stub] BookingCancelled. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class BookingDisputedConsumer : IConsumer<BookingDisputedEvent>
{
    private readonly ILogger<BookingDisputedConsumer> _logger;
    public BookingDisputedConsumer(ILogger<BookingDisputedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<BookingDisputedEvent> context) { _logger.LogInformation("[Phase 3 stub] BookingDisputed. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class SessionReminderConsumer : IConsumer<SessionReminderEvent>
{
    private readonly ILogger<SessionReminderConsumer> _logger;
    public SessionReminderConsumer(ILogger<SessionReminderConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<SessionReminderEvent> context) { _logger.LogInformation("[Phase 3 stub] SessionReminder. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class SessionNoShowConsumer : IConsumer<SessionNoShowEvent>
{
    private readonly ILogger<SessionNoShowConsumer> _logger;
    public SessionNoShowConsumer(ILogger<SessionNoShowConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<SessionNoShowEvent> context) { _logger.LogInformation("[Phase 3 stub] SessionNoShow. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class PayoutReleasedConsumer : IConsumer<PayoutReleasedEvent>
{
    private readonly ILogger<PayoutReleasedConsumer> _logger;
    public PayoutReleasedConsumer(ILogger<PayoutReleasedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<PayoutReleasedEvent> context) { _logger.LogInformation("[Phase 3 stub] PayoutReleased. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class PayoutFailedConsumer : IConsumer<PayoutFailedEvent>
{
    private readonly ILogger<PayoutFailedConsumer> _logger;
    public PayoutFailedConsumer(ILogger<PayoutFailedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<PayoutFailedEvent> context) { _logger.LogInformation("[Phase 3 stub] PayoutFailed. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}

public class RefundIssuedConsumer : IConsumer<RefundIssuedEvent>
{
    private readonly ILogger<RefundIssuedConsumer> _logger;
    public RefundIssuedConsumer(ILogger<RefundIssuedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<RefundIssuedEvent> context) { _logger.LogInformation("[Phase 3 stub] RefundIssued. BookingId: {BookingId}", context.Message.BookingId); return Task.CompletedTask; }
}
