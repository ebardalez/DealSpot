using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.Reputation;

/// <summary>
/// -3 rep when a community-submitted deal expires (CommunityReported or UrlDead).
/// </summary>
public sealed class UpdateReputationOnExpireHandler(
    IDealRepository dealRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateReputationOnExpireHandler> logger) : INotificationHandler<DealExpiredDomainEvent>
{
    public async Task Handle(DealExpiredDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notification.Reason is not (DealExpiredReason.CommunityReported or DealExpiredReason.UrlDead))
                return;

            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal?.SubmittedByUserId is null) return;

            var user = await userRepository.GetByIdAsync(deal.SubmittedByUserId.Value, cancellationToken);
            if (user is null) return;

            user.AddReputation(-3);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update reputation on expire for deal {DealId}", notification.DealId);
        }
    }
}
