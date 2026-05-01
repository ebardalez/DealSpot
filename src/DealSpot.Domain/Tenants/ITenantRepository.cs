namespace DealSpot.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    void Add(Tenant tenant);
}
