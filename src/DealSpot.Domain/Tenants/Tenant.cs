using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Tenants;

public sealed class Tenant : Entity
{
    private Tenant() { }

    private Tenant(Guid id, string slug, string name, string domain, TenantBranding branding) : base(id)
    {
        Slug = slug;
        Name = name;
        Domain = domain;
        Branding = branding;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public TenantBranding Branding { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Result<Tenant> Create(string slug, string name, string domain, TenantBranding branding)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<Tenant>(TenantErrors.InvalidSlug);
        if (string.IsNullOrWhiteSpace(domain))
            return Result.Failure<Tenant>(TenantErrors.InvalidDomain);

        return Result.Success(new Tenant(Guid.NewGuid(), slug.ToLowerInvariant(), name, domain, branding));
    }

    public void UpdateBranding(TenantBranding branding) => Branding = branding;
    public void Deactivate() => IsActive = false;
}
