using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.AffiliateLinks;

public sealed class AffiliateNetwork : Entity
{
    private AffiliateNetwork() { }

    private AffiliateNetwork(
        Guid id, Guid tenantId, string retailerDomain,
        string networkName, string affiliateTag, string urlTemplate) : base(id)
    {
        TenantId = tenantId;
        RetailerDomain = retailerDomain;
        NetworkName = networkName;
        AffiliateTag = affiliateTag;
        UrlTemplate = urlTemplate;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string RetailerDomain { get; private set; } = string.Empty;
    public string NetworkName { get; private set; } = string.Empty;
    public string AffiliateTag { get; private set; } = string.Empty;
    /// <summary>Use {url} and {tag} as placeholders. Amazon: "{url}?tag={tag}"</summary>
    public string UrlTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static AffiliateNetwork Create(
        Guid tenantId, string retailerDomain, string networkName,
        string affiliateTag, string urlTemplate) =>
        new(Guid.NewGuid(), tenantId, retailerDomain.ToLowerInvariant(),
            networkName, affiliateTag, urlTemplate);

    public string BuildAffiliateUrl(string originalUrl) =>
        UrlTemplate.Replace("{url}", originalUrl).Replace("{tag}", AffiliateTag);
}
