using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

internal sealed class PushSubscriptionRepository(ApplicationDbContext context) : IPushSubscriptionRepository
{
    public async Task<IReadOnlyList<PushSubscription>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.PushSubscriptions
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PushSubscription>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.PushSubscriptions
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default) =>
        await context.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == endpoint, cancellationToken);

    public void Add(PushSubscription subscription) => context.PushSubscriptions.Add(subscription);
    public void Remove(PushSubscription subscription) => context.PushSubscriptions.Remove(subscription);
}
