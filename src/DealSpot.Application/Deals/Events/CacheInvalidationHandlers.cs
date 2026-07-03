using DealSpot.Application.Abstractions.Caching;
using DealSpot.Domain.Deals.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.Events;

// ponytail: tag eviction only — no per-slug surgical invalidation. Add finer keys only if eviction proves too broad.

public sealed class DealCreatedCacheInvalidationHandler(
    ICacheInvalidator cacheInvalidator,
    ILogger<DealCreatedCacheInvalidationHandler> logger) : INotificationHandler<DealCreatedDomainEvent>
{
    public async Task Handle(DealCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await cacheInvalidator.EvictAsync("deals", cancellationToken);
            await cacheInvalidator.EvictAsync("categories", cancellationToken);
        }
        catch (Exception ex)
        {
            // ponytail: eviction failure must not fail the deal write
            logger.LogWarning(ex, "Cache eviction failed after deal {DealId} created", notification.DealId);
        }
    }
}

public sealed class DealExpiredCacheInvalidationHandler(
    ICacheInvalidator cacheInvalidator,
    ILogger<DealExpiredCacheInvalidationHandler> logger) : INotificationHandler<DealExpiredDomainEvent>
{
    public async Task Handle(DealExpiredDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await cacheInvalidator.EvictAsync("deals", cancellationToken);
            await cacheInvalidator.EvictAsync("categories", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache eviction failed after deal {DealId} expired", notification.DealId);
        }
    }
}
