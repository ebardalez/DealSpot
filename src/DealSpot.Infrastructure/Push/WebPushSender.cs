using System.Text.Json;
using DealSpot.Application.Abstractions.Push;
using DealSpot.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;
using PushSub = DealSpot.Domain.Users.PushSubscription;

namespace DealSpot.Infrastructure.Push;

public sealed class WebPushSender(
    IOptions<VapidOptions> vapidOptions,
    ILogger<WebPushSender> logger) : IPushSender
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<bool> SendAsync(PushSub subscription, PushPayload payload, CancellationToken cancellationToken = default)
    {
        var vapid = vapidOptions.Value;
        if (string.IsNullOrEmpty(vapid.PublicKey) || string.IsNullOrEmpty(vapid.PrivateKey))
        {
            logger.LogWarning("VAPID keys not configured — skipping push notification");
            return true; // Don't prune on config issue
        }

        var client = new WebPushClient();
        client.SetVapidDetails(vapid.Subject, vapid.PublicKey, vapid.PrivateKey);

        var webPushSub = new WebPush.PushSubscription(
            subscription.Endpoint,
            subscription.P256dh,
            subscription.Auth);

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        try
        {
            await client.SendNotificationAsync(webPushSub, json);
            return true;
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            logger.LogInformation("Push subscription expired (410 Gone): {Endpoint}", subscription.Endpoint);
            return false; // Caller should remove
        }
        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Push subscription not found (404): {Endpoint}", subscription.Endpoint);
            return false;
        }
        catch (Exception ex)
        {
            // ponytail: log and move on, don't fail the whole notification pipeline
            logger.LogError(ex, "Failed to send push notification to {Endpoint}", subscription.Endpoint);
            return true; // Don't prune on transient errors
        }
    }
}
