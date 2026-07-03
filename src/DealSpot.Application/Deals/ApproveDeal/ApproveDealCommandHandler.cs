using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.ApproveDeal;

public sealed record ApproveDealCommand(Guid DealId) : ICommand;

public sealed class ApproveDealCommandHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveDealCommand>
{
    public async Task<Result> Handle(ApproveDealCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null) return Result.Failure(DealErrors.NotFound);

        var result = deal.Approve();
        if (result.IsFailure) return result;

        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
