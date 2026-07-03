using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class WatchlistRepository(ApplicationDbContext dbContext) : IWatchlistRepository
{
    public async Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.WatchlistEntries.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<WatchlistEntry>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.WatchlistEntries
            .Where(w => w.UserId == userId && w.TenantId == tenantId && w.IsActive)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, Guid? categoryId, string? keyword, CancellationToken cancellationToken = default) =>
        await dbContext.WatchlistEntries.AnyAsync(w =>
            w.UserId == userId && w.TenantId == tenantId && w.IsActive &&
            w.CategoryId == categoryId && w.ProductKeyword == keyword, cancellationToken);

    public async Task<IReadOnlyList<WatchlistEntry>> GetCategoryWatchersAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default) =>
        await dbContext.WatchlistEntries
            .Where(w => w.TenantId == tenantId && w.CategoryId == categoryId && w.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WatchlistEntry>> GetKeywordWatchersAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.WatchlistEntries
            .Where(w => w.TenantId == tenantId && w.ProductKeyword != null && w.IsActive)
            .ToListAsync(cancellationToken);

    public void Add(WatchlistEntry entry) => dbContext.WatchlistEntries.Add(entry);
    public void Remove(WatchlistEntry entry) => dbContext.WatchlistEntries.Remove(entry);
}
