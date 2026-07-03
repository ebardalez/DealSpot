using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

/// <summary>
/// Queued alert for digest-mode users. Processed by DailyDigestJob.
/// ponytail: in-DB table, no message broker. Add a broker only if volume outgrows a daily batch query.
/// </summary>
public sealed class PendingAlert : Entity
{
    private PendingAlert() { }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DealId { get; private set; }
    public string AlertType { get; private set; } = string.Empty; // "NewDealInCategory" or "WatchlistMatch"
    public string? MatchedKeyword { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsSent { get; private set; }

    public static PendingAlert Create(Guid userId, Guid tenantId, Guid dealId, string alertType, string? matchedKeyword = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            DealId = dealId,
            AlertType = alertType,
            MatchedKeyword = matchedKeyword,
            CreatedAtUtc = DateTime.UtcNow,
            IsSent = false
        };

    public void MarkSent() => IsSent = true;
}
