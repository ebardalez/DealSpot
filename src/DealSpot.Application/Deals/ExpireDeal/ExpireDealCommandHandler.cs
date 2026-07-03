using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.ExpireDeal;

public sealed record ExpireDealCommand(Guid DealId) : ICommand;

public sealed class ExpireDealCommandHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ExpireDealCommand>
{
    public async Task<Result> Handle(ExpireDealCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null) return Result.Failure(DealErrors.NotFound);

        deal.Expire(DealExpiredReason.Manual);
        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
