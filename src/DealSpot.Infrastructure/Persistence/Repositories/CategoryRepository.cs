using DealSpot.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Categories.FindAsync([id], cancellationToken);

    public async Task<Category?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default) =>
        await dbContext.Categories.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);
}

