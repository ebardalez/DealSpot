using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

public static class WatchlistErrors
{
    public static readonly Error Duplicate = new("Watchlist.Duplicate", "You are already watching this.");
    public static readonly Error NotFound = new("Watchlist.NotFound", "Watchlist entry was not found.");
    public static readonly Error NotOwner = new("Watchlist.NotOwner", "You do not own this watchlist entry.");
}
