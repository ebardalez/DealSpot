namespace DealSpot.Domain.Tenants;

public sealed record TenantBranding(
    string PrimaryColor,
    string SecondaryColor,
    string LogoUrl,
    string FaviconUrl,
    string MetaDescription,
    string MetaKeywords);
