namespace DealSpot.Domain.Deals;

public interface IPriceSnapshotRepository
{
    Task<IReadOnlyList<PriceSnapshot>> GetByDealAsync(Guid dealId, CancellationToken cancellationToken = default);
    void Add(PriceSnapshot snapshot);
}
