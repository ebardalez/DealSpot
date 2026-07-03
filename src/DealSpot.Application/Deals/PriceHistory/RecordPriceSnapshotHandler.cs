using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.PriceHistory;

// ponytail: one handler class, two INotificationHandler impls. Records snapshot on create + price change.
public sealed class RecordPriceSnapshotHandler(
    IDealRepository dealRepository,
    IPriceSnapshotRepository snapshotRepository,
    IUnitOfWork unitOfWork,
    ILogger<RecordPriceSnapshotHandler> logger)
    : INotificationHandler<DealCreatedDomainEvent>,
      INotificationHandler<DealPriceChangedDomainEvent>
{
    public async Task Handle(DealCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal is null) return;

            snapshotRepository.Add(PriceSnapshot.Record(
                deal.Id, deal.Price.CurrentPrice, deal.Price.OriginalPrice, deal.Price.Currency, "initial"));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record initial price snapshot for deal {DealId}", notification.DealId);
        }
    }

    public async Task Handle(DealPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            snapshotRepository.Add(PriceSnapshot.Record(
                notification.DealId, notification.NewPrice, notification.NewOriginalPrice, notification.Currency, "admin_update"));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record price snapshot for deal {DealId}", notification.DealId);
        }
    }
}
