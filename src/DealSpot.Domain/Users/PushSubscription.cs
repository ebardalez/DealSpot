using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

public sealed class PushSubscription : Entity
{
    private PushSubscription() { }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static PushSubscription Create(Guid userId, Guid tenantId, string endpoint, string p256dh, string auth) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            CreatedAtUtc = DateTime.UtcNow
        };
}
