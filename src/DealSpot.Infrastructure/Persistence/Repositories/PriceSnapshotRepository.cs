using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

internal sealed class PriceSnapshotRepository(ApplicationDbContext dbContext) : IPriceSnapshotRepository
{
    public async Task<IReadOnlyList<PriceSnapshot>> GetByDealAsync(Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.PriceSnapshots
            .Where(s => s.DealId == dealId)
            .OrderBy(s => s.RecordedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(PriceSnapshot snapshot) => dbContext.PriceSnapshots.Add(snapshot);
}
