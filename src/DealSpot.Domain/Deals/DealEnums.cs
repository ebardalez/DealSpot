namespace DealSpot.Domain.Deals;

public enum DealStatus
{
    Active = 1,
    Expired = 2,
    PendingApproval = 3,
    Rejected = 4
}

public enum DealExpiredReason
{
    Manual = 1,
    CommunityReported = 2,
    UrlDead = 3,
    Scheduled = 4
}
