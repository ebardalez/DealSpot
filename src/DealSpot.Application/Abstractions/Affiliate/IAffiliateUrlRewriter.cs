namespace DealSpot.Application.Abstractions.Affiliate;

public interface IAffiliateUrlRewriter
{
    Task<AffiliateRewriteResult> RewriteAsync(Guid tenantId, string rawUrl, CancellationToken cancellationToken = default);
}

public sealed record AffiliateRewriteResult(
    string AffiliateUrl,
    string CanonicalUrl,
    string RetailerDomain,
    bool WasRewritten);
