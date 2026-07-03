using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class DealRepository(ApplicationDbContext dbContext) : IDealRepository
{
    public async Task<Deal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Deals.FindAsync([id], cancellationToken);

    public async Task<Deal?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default) =>
        await dbContext.Deals.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Deal>> GetActiveByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Deals
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Active)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Deal>> GetActiveByCategoryAsync(Guid tenantId, Guid categoryId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Deals
            .Where(d => d.TenantId == tenantId && d.CategoryId == categoryId && d.Status == DealStatus.Active)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Deal>> GetActiveDealsForUrlCheckAsync(int batchSize, CancellationToken cancellationToken = default) =>
        await dbContext.Deals
            .Where(d => d.Status == DealStatus.Active)
            .OrderBy(d => d.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public void Add(Deal deal) => dbContext.Deals.Add(deal);

    public async Task<IReadOnlyList<Deal>> GetByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.Deals
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Deal>> GetPendingByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.Deals
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.PendingApproval)
            .OrderBy(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Update(Deal deal) => dbContext.Deals.Update(deal);
}
