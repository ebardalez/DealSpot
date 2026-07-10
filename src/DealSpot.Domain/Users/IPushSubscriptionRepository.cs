namespace DealSpot.Domain.Users;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PushSubscription>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    void Add(PushSubscription subscription);
    void Remove(PushSubscription subscription);
}
