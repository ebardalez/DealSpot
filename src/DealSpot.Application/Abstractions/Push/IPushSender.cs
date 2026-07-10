using DealSpot.Domain.Users;

namespace DealSpot.Application.Abstractions.Push;

public sealed record PushPayload(string Title, string Body, string Url, string? IconUrl = null);

public interface IPushSender
{
    /// <summary>
    /// Sends a push notification. Returns false if the subscription is expired (410 Gone) and should be removed.
    /// </summary>
    Task<bool> SendAsync(PushSubscription subscription, PushPayload payload, CancellationToken cancellationToken = default);
}
