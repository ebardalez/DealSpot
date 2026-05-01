namespace DealSpot.Domain.Deals;

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Deal?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Deal>> GetActiveByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Deal>> GetActiveByCategoryAsync(Guid tenantId, Guid categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Deal>> GetActiveDealsForUrlCheckAsync(int batchSize, CancellationToken cancellationToken = default);
    void Add(Deal deal);
    void Update(Deal deal);
}
