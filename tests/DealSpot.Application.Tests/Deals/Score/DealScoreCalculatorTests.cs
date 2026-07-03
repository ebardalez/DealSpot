using DealSpot.Application.Deals.Score;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Score;

public sealed class DealScoreCalculatorTests
{
    private static Deal CreateDeal(decimal current, decimal? original, string retailer)
    {
        var price = new DealPrice(current, original, "USD");
        return Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test", "test-slug", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, retailer).Value;
    }

    [Fact]
    public void Calculate_HighDiscount_KnownRetailer_ScoresHigh()
    {
        var deal = CreateDeal(25m, 100m, "Amazon"); // 75% discount

        var score = DealScoreCalculator.Calculate(deal);

        score.IsCalculated.Should().BeTrue();
        score.Value.Should().Be(67); // 100*0.4 + 90*0.3 + 0*0.3 = 67
        score.DiscountComponent.Should().Be(100m); // 75% > 50% cap
        score.RetailerTrustComponent.Should().Be(90m); // Amazon
    }

    [Fact]
    public void Calculate_NoDiscount_UnknownRetailer_ScoresLow()
    {
        var deal = CreateDeal(50m, null, "SomeRandomShop");

        var score = DealScoreCalculator.Calculate(deal);

        score.IsCalculated.Should().BeTrue();
        score.Value.Should().BeLessThanOrEqualTo(30); // 0*0.4 + 50*0.3 + 0*0.3 = 15
        score.DiscountComponent.Should().Be(0m);
        score.RetailerTrustComponent.Should().Be(50m);
    }

    [Fact]
    public void Calculate_WithNetVotes_IncludesVelocity()
    {
        var deal = CreateDeal(30m, 60m, "Best Buy"); // 50% discount
        // Simulate 10 net votes
        for (int i = 0; i < 10; i++) deal.ApplyVote(1);

        var score = DealScoreCalculator.Calculate(deal);

        score.VelocityComponent.Should().Be(50m); // 10/20 * 100 = 50
        score.Value.Should().BeGreaterThan(50);
    }
}
