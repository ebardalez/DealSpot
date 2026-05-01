namespace DealSpot.Domain.Deals.ValueObjects;

public sealed record DealPrice(decimal CurrentPrice, decimal? OriginalPrice, string Currency)
{
    public decimal? DiscountPercentage => OriginalPrice.HasValue && OriginalPrice > 0
        ? Math.Round((1 - CurrentPrice / OriginalPrice.Value) * 100, 1)
        : null;

    public bool HasDiscount => DiscountPercentage.HasValue && DiscountPercentage > 0;
}

public sealed record DealScore(
    int Value,
    decimal DiscountComponent,
    decimal RetailerTrustComponent,
    decimal VelocityComponent,
    DateTime CalculatedAtUtc)
{
    public static readonly DealScore NotCalculated = new(0, 0, 0, 0, DateTime.MinValue);

    public bool IsCalculated => CalculatedAtUtc > DateTime.MinValue;

    public string Label => Value switch
    {
        >= 85 => "🔥 Excellent",
        >= 70 => "👍 Great",
        >= 50 => "✅ Good",
        >= 30 => "😐 Fair",
        _ => "⚠️ Weak"
    };
}
