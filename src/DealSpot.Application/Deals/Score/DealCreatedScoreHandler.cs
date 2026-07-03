using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.Score;

// ponytail: calculates initial score on creation (velocity = 0 for new deals). Hangfire job recalculates later.
public sealed class DealCreatedScoreHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork,
    ILogger<DealCreatedScoreHandler> logger) : INotificationHandler<DealCreatedDomainEvent>
{
    public async Task Handle(DealCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal is null) return;

            var score = DealScoreCalculator.Calculate(deal);
            deal.UpdateDealScore(score);
            dealRepository.Update(deal);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // ponytail: score failure must not fail the deal write
            logger.LogWarning(ex, "Failed to calculate initial score for deal {DealId}", notification.DealId);
        }
    }
}
