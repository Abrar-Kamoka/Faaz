using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

// Turns TryAutoActivateAsync's "did this call cause activation" bool into the welcome-notification
// event, in one place — called from every command handler that can trigger auto-activation (7 call
// sites), so the publish logic can't drift between them the way the profile-stub mapping once did.
internal static class ConsultantActivationPublisher
{
    public static Task PublishIfActivatedAsync(
        bool wasActivated,
        Domain.Entities.ConsultantProfile profile,
        IPublishEndpoint publishEndpoint,
        CancellationToken ct)
    {
        return wasActivated
            ? publishEndpoint.Publish(new ConsultantProfileActivatedEvent(profile.UserId, profile.DisplayName), ct)
            : Task.CompletedTask;
    }
}
