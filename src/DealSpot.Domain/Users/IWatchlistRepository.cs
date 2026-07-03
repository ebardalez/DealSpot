namespace DealSpot.Domain.Users;

public interface IWatchlistRepository
{
    Task<WatchlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistEntry>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid tenantId, Guid? categoryId, string? keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistEntry>> GetCategoryWatchersAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchlistEntry>> GetKeywordWatchersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    void Add(WatchlistEntry entry);
    void Remove(WatchlistEntry entry);
}
