using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Tenants;

public static class TenantErrors
{
    public static readonly Error NotFound = new("Tenant.NotFound", "Tenant was not found.");
    public static readonly Error InvalidSlug = new("Tenant.InvalidSlug", "Tenant slug cannot be empty.");
    public static readonly Error InvalidDomain = new("Tenant.InvalidDomain", "Tenant domain cannot be empty.");
    public static readonly Error SlugAlreadyExists = new("Tenant.SlugAlreadyExists", "A tenant with this slug already exists.");
}
