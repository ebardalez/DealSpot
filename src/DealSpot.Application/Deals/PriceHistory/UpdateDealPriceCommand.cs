using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.PriceHistory;

public sealed record UpdateDealPriceCommand(Guid DealId, decimal NewCurrentPrice, decimal? NewOriginalPrice) : ICommand;

public sealed class UpdateDealPriceCommandHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateDealPriceCommand>
{
    public async Task<Result> Handle(UpdateDealPriceCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null)
            return Result.Failure(DealErrors.NotFound);

        var result = deal.UpdatePrice(command.NewCurrentPrice, command.NewOriginalPrice);
        if (result.IsFailure)
            return result;

        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
