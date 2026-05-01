using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Deals;

public static class DealErrors
{
    public static readonly Error NotFound = new("Deal.NotFound", "Deal was not found.");
    public static readonly Error InvalidTitle = new("Deal.InvalidTitle", "Deal title cannot be empty.");
    public static readonly Error InvalidUrl = new("Deal.InvalidUrl", "Deal URL cannot be empty.");
    public static readonly Error InvalidPrice = new("Deal.InvalidPrice", "Deal price must be greater than zero.");
    public static readonly Error AlreadyExpired = new("Deal.AlreadyExpired", "Deal is already expired.");
    public static readonly Error Unauthorized = new("Deal.Unauthorized", "You do not have permission to modify this deal.");
}
