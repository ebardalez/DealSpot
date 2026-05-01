using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

public sealed class WatchlistEntry : Entity
{
    private WatchlistEntry() { }

    private WatchlistEntry(
        Guid id, Guid userId, Guid tenantId,
        Guid? categoryId, string? productKeyword, decimal? targetPrice) : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        CategoryId = categoryId;
        ProductKeyword = productKeyword;
        TargetPrice = targetPrice;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? ProductKeyword { get; private set; }
    public decimal? TargetPrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static WatchlistEntry CreateCategoryWatch(Guid userId, Guid tenantId, Guid categoryId) =>
        new(Guid.NewGuid(), userId, tenantId, categoryId, null, null);

    public static WatchlistEntry CreateKeywordWatch(Guid userId, Guid tenantId, string keyword, decimal? targetPrice = null) =>
        new(Guid.NewGuid(), userId, tenantId, null, keyword, targetPrice);

    public void Deactivate() => IsActive = false;
}

public sealed class UserNotificationPreference : Entity
{
    private UserNotificationPreference() { }

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public bool NewDealInCategory { get; private set; } = true;
    public bool WatchlistMatch { get; private set; } = true;
    public bool DealExpiringSoon { get; private set; } = false;
    public NotificationFrequency Frequency { get; private set; } = NotificationFrequency.Instant;

    public static UserNotificationPreference Create(Guid userId, Guid tenantId) =>
        new() { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId };

    public void Update(bool newDealInCategory, bool watchlistMatch, bool dealExpiringSoon, NotificationFrequency frequency)
    {
        NewDealInCategory = newDealInCategory;
        WatchlistMatch = watchlistMatch;
        DealExpiringSoon = dealExpiringSoon;
        Frequency = frequency;
    }
}

public enum NotificationFrequency { Instant = 1, DailyDigest = 2 }
