using DealSpot.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(ApplicationDbContext dbContext) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants.FindAsync([id], cancellationToken);

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants.FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive, cancellationToken);

    public async Task<Tenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default) =>
        await dbContext.Tenants.FirstOrDefaultAsync(t => t.Domain == domain && t.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tenants.Where(t => t.IsActive).ToListAsync(cancellationToken);

    public void Add(Tenant tenant) => dbContext.Tenants.Add(tenant);
}
