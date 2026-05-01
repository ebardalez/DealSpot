using DealSpot.Domain.Tenants;

namespace DealSpot.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Tenant Current { get; }
    bool IsResolved { get; }
}
