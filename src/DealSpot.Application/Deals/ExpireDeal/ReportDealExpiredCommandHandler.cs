using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.ExpireDeal;

public sealed record ReportDealExpiredCommand(Guid DealId) : ICommand;

public sealed class ReportDealExpiredCommandHandler(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReportDealExpiredCommand>
{
    public async Task<Result> Handle(ReportDealExpiredCommand command, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null) return Result.Failure(DealErrors.NotFound);

        var result = deal.ReportAsExpired();
        if (result.IsFailure) return result;

        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
