namespace DealSpot.Domain.Categories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    void Add(Category category);
}
