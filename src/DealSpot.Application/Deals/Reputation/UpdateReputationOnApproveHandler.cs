using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.Reputation;

/// <summary>
/// +10 rep to submitter when their deal goes live (fires on approve or admin-attributed create).
/// </summary>
public sealed class UpdateReputationOnApproveHandler(
    IDealRepository dealRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateReputationOnApproveHandler> logger) : INotificationHandler<DealCreatedDomainEvent>
{
    public async Task Handle(DealCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal?.SubmittedByUserId is null) return;

            var user = await userRepository.GetByIdAsync(deal.SubmittedByUserId.Value, cancellationToken);
            if (user is null) return;

            user.AddReputation(10);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // ponytail: rep failure must not fail the deal write
            logger.LogWarning(ex, "Failed to update reputation for deal {DealId}", notification.DealId);
        }
    }
}
