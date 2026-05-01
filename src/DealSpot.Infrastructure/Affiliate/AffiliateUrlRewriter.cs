using DealSpot.Application.Abstractions.Affiliate;
using DealSpot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Affiliate;

public sealed class AffiliateUrlRewriter(ApplicationDbContext dbContext) : IAffiliateUrlRewriter
{
    private static readonly HashSet<string> TrackingParams =
    [
        "ref", "tag", "linkCode", "linkId", "camp", "creative",
        "utm_source", "utm_medium", "utm_campaign", "utm_content", "utm_term",
        "cid", "sid", "affid", "aff_id", "affiliate"
    ];

    public async Task<AffiliateRewriteResult> RewriteAsync(Guid tenantId, string rawUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return new AffiliateRewriteResult(rawUrl, rawUrl, string.Empty, false);

        var domain = uri.Host.ToLowerInvariant().TrimStart('w', '.');
        var canonicalUrl = BuildCanonicalUrl(uri);

        var network = await dbContext.AffiliateNetworks
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.RetailerDomain == domain && n.IsActive, cancellationToken);

        if (network is null)
            return new AffiliateRewriteResult(canonicalUrl, canonicalUrl, domain, false);

        return new AffiliateRewriteResult(network.BuildAffiliateUrl(canonicalUrl), canonicalUrl, domain, true);
    }

    private static string BuildCanonicalUrl(Uri uri)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        foreach (var param in TrackingParams) query.Remove(param);
        var builder = new UriBuilder(uri) { Query = query.Count > 0 ? query.ToString() : string.Empty };
        return builder.Uri.ToString().TrimEnd('?');
    }
}
