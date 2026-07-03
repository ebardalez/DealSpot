using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Deals;

// ponytail: flat entity, not aggregate root. No domain events — snapshots are a side effect of Deal events.
public sealed class PriceSnapshot : Entity
{
    private PriceSnapshot() { }

    public Guid DealId { get; private set; }
    public decimal Price { get; private set; }
    public decimal? OriginalPrice { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty; // "initial", "admin_update", "scraper"
    public DateTime RecordedAtUtc { get; private set; }

    public static PriceSnapshot Record(Guid dealId, decimal price, decimal? originalPrice, string currency, string source) =>
        new()
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Price = price,
            OriginalPrice = originalPrice,
            Currency = currency,
            Source = source,
            RecordedAtUtc = DateTime.UtcNow
        };
}
