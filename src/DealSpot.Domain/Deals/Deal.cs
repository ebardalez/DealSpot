using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;

namespace DealSpot.Domain.Deals;

public sealed class Deal : Entity
{
    private Deal() { }

    private Deal(
        Guid id, Guid tenantId, Guid categoryId, Guid? submittedByUserId,
        string title, string slug, string description, DealPrice price,
        string originalUrl, string affiliateUrl, string? imageUrl, string retailerName) : base(id)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        SubmittedByUserId = submittedByUserId;
        Title = title;
        Slug = slug;
        Description = description;
        Price = price;
        OriginalUrl = originalUrl;
        AffiliateUrl = affiliateUrl;
        ImageUrl = imageUrl;
        RetailerName = retailerName;
        Status = DealStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiredReportCount = 0;
        DealScore = DealScore.NotCalculated;
    }

    public Guid TenantId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DealPrice Price { get; private set; } = null!;
    public string OriginalUrl { get; private set; } = string.Empty;
    public string AffiliateUrl { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string RetailerName { get; private set; } = string.Empty;
    public DealStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public int ExpiredReportCount { get; private set; }
    public DealScore DealScore { get; private set; } = DealScore.NotCalculated;
    public int UpVoteCount { get; private set; }
    public int DownVoteCount { get; private set; }
    public int NetScore => UpVoteCount - DownVoteCount;

    public static Result<Deal> Create(
        Guid tenantId, Guid categoryId, Guid? submittedByUserId,
        string title, string slug, string description, DealPrice price,
        string originalUrl, string affiliateUrl, string? imageUrl, string retailerName)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Deal>(DealErrors.InvalidTitle);
        if (string.IsNullOrWhiteSpace(affiliateUrl))
            return Result.Failure<Deal>(DealErrors.InvalidUrl);
        if (price.CurrentPrice <= 0)
            return Result.Failure<Deal>(DealErrors.InvalidPrice);

        var deal = new Deal(Guid.NewGuid(), tenantId, categoryId, submittedByUserId,
            title, slug, description, price, originalUrl, affiliateUrl, imageUrl, retailerName);

        deal.RaiseDomainEvent(new DealCreatedDomainEvent(deal.Id, deal.TenantId));
        return Result.Success(deal);
    }

    public Result ReportAsExpired()
    {
        if (Status == DealStatus.Expired)
            return Result.Failure(DealErrors.AlreadyExpired);

        ExpiredReportCount++;
        if (ExpiredReportCount >= 5)
            Expire(DealExpiredReason.CommunityReported);

        return Result.Success();
    }

    public void Expire(DealExpiredReason reason)
    {
        if (Status == DealStatus.Expired) return;
        Status = DealStatus.Expired;
        ExpiresAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DealExpiredDomainEvent(Id, TenantId, reason));
    }

    public void UpdateDealScore(DealScore score) => DealScore = score;

    public void UpdateAffiliateUrl(string affiliateUrl)
    {
        if (!string.IsNullOrWhiteSpace(affiliateUrl))
            AffiliateUrl = affiliateUrl;
    }

    public void ApplyVote(int value)
    {
        if (value > 0) UpVoteCount++;
        else DownVoteCount++;
    }

    public void RemoveVote(int value)
    {
        if (value > 0) UpVoteCount = Math.Max(0, UpVoteCount - 1);
        else DownVoteCount = Math.Max(0, DownVoteCount - 1);
    }
}
