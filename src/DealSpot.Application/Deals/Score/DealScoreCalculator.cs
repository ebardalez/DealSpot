using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;

namespace DealSpot.Application.Deals.Score;

// ponytail: static calculator, no interface. Add IDealScoreCalculator only if scoring becomes pluggable per-tenant.
public static class DealScoreCalculator
{
    private const double DiscountWeight = 0.4;
    private const double RetailerWeight = 0.3;
    private const double VelocityWeight = 0.3;

    public static DealScore Calculate(Deal deal)
    {
        var discount = DiscountComponent(deal.Price);
        var retailer = RetailerTrustComponent(deal.RetailerName);
        var velocity = VelocityComponent(deal.NetScore);

        var value = (int)Math.Round(discount * DiscountWeight + retailer * RetailerWeight + velocity * VelocityWeight);
        value = Math.Clamp(value, 0, 100);

        return new DealScore(value, (decimal)discount, (decimal)retailer, (decimal)velocity, DateTime.UtcNow);
    }

    // 50%+ discount = perfect 100. Linear scale below.
    private static double DiscountComponent(DealPrice price)
    {
        var pct = (double)(price.DiscountPercentage ?? 0);
        return Math.Clamp(pct / 50.0 * 100.0, 0, 100);
    }

    // ponytail: hardcoded trust map. Move to DB/config when retailers are user-managed.
    private static double RetailerTrustComponent(string retailerName)
    {
        var name = retailerName.Trim().ToLowerInvariant();
        return name switch
        {
            "amazon" or "amazon.com" => 90,
            "walmart" or "walmart.com" => 85,
            "best buy" or "bestbuy" or "bestbuy.com" => 85,
            "target" or "target.com" => 80,
            "newegg" or "newegg.com" => 75,
            "b&h" or "b&h photo" or "bhphotovideo.com" => 80,
            "costco" or "costco.com" => 85,
            "chewy" or "chewy.com" => 80,
            "petco" or "petco.com" => 75,
            "bulk reef supply" or "bulkreefsupply.com" => 80,
            "marine depot" or "marinedepot.com" => 75,
            "iherb" or "iherb.com" => 75,
            "vitacost" or "vitacost.com" => 70,
            _ => 50
        };
    }

    // 20+ net votes = perfect 100. Negative clamped to 0.
    private static double VelocityComponent(int netScore) =>
        Math.Clamp(Math.Max(netScore, 0) / 20.0 * 100.0, 0, 100);
}
