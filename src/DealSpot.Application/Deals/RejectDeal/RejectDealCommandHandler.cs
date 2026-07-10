using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Users;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.RejectDeal;

public sealed record RejectDealCommand(Guid DealId) : ICommand;

public sealed class RejectDealCommandHandler(
    IDealRepository dealRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<RejectDealCommandHandler> logger) : ICommandHandler<RejectDealCommand>
{
    public async Task<Result> Handle(RejectDealCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null) return Result.Failure(DealErrors.NotFound);

        var result = deal.Reject();
        if (result.IsFailure) return result;

        dealRepository.Update(deal);

        // ponytail: -2 rep inline, no domain event needed for reject
        if (deal.SubmittedByUserId is not null)
        {
            try
            {
                var user = await userRepository.GetByIdAsync(deal.SubmittedByUserId.Value, cancellationToken);
                if (user is not null)
                {
                    user.AddReputation(-2);
                    userRepository.Update(user);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deduct reputation on reject for deal {DealId}", command.DealId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
