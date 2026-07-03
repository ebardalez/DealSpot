using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.RejectDeal;

public sealed record RejectDealCommand(Guid DealId) : ICommand;

public sealed class RejectDealCommandHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RejectDealCommand>
{
    public async Task<Result> Handle(RejectDealCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null) return Result.Failure(DealErrors.NotFound);

        var result = deal.Reject();
        if (result.IsFailure) return result;

        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
