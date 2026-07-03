using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.PriceHistory;

public sealed record GetPriceHistoryQuery(Guid DealId) : IQuery<IReadOnlyList<PriceSnapshotDto>>;

public sealed record PriceSnapshotDto(decimal Price, decimal? OriginalPrice, string Currency, string Source, DateTime RecordedAtUtc);

public sealed class GetPriceHistoryQueryHandler(
    IPriceSnapshotRepository snapshotRepository) : IQueryHandler<GetPriceHistoryQuery, IReadOnlyList<PriceSnapshotDto>>
{
    public async Task<Result<IReadOnlyList<PriceSnapshotDto>>> Handle(GetPriceHistoryQuery query, CancellationToken cancellationToken)
    {
        var snapshots = await snapshotRepository.GetByDealAsync(query.DealId, cancellationToken);

        var dtos = snapshots
            .Select(s => new PriceSnapshotDto(s.Price, s.OriginalPrice, s.Currency, s.Source, s.RecordedAtUtc))
            .ToList();

        return Result.Success<IReadOnlyList<PriceSnapshotDto>>(dtos);
    }
}
