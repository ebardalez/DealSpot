using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;

namespace DealSpot.Domain.Tests.Deals;

public sealed class DealTests
{
    private static Deal CreateValidDeal() =>
        Deal.Create(
            tenantId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            submittedByUserId: null,
            title: "Apple AirPods Pro 2nd Gen",
            slug: "apple-airpods-pro-2nd-gen-179",
            description: "Best price we've seen this year.",
            price: new DealPrice(179.00m, 249.00m, "USD"),
            originalUrl: "https://amazon.com/dp/B0BDHWDR12",
            affiliateUrl: "https://amazon.com/dp/B0BDHWDR12?tag=dealspot-20",
            imageUrl: null,
            retailerName: "Amazon").Value;

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test Deal", "test-deal-10", "A test deal",
            new DealPrice(10m, 20m, "USD"),
            "https://example.com/product",
            "https://example.com/product?tag=test",
            null, "TestRetailer");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DealStatus.Active);
        result.Value.ExpiredReportCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldFail()
    {
        var result = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "", "slug", "desc",
            new DealPrice(10m, null, "USD"),
            "https://example.com", "https://example.com", null, "Retailer");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.InvalidTitle);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldFail()
    {
        var result = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Valid Title", "slug", "desc",
            new DealPrice(0m, null, "USD"),
            "https://example.com", "https://example.com", null, "Retailer");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.InvalidPrice);
    }

    [Fact]
    public void ReportAsExpired_BeforeThreshold_ShouldIncrementCountOnly()
    {
        var deal = CreateValidDeal();

        for (int i = 0; i < 4; i++)
            deal.ReportAsExpired();

        deal.ExpiredReportCount.Should().Be(4);
        deal.Status.Should().Be(DealStatus.Active);
    }

    [Fact]
    public void ReportAsExpired_AtFiveReports_ShouldAutoExpire()
    {
        var deal = CreateValidDeal();

        for (int i = 0; i < 5; i++)
            deal.ReportAsExpired();

        deal.Status.Should().Be(DealStatus.Expired);
        deal.ExpiresAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ReportAsExpired_WhenAlreadyExpired_ShouldReturnFailure()
    {
        var deal = CreateValidDeal();
        deal.Expire(DealExpiredReason.Manual);

        var result = deal.ReportAsExpired();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.AlreadyExpired);
    }

    [Fact]
    public void DealPrice_ShouldCalculateDiscountPercentageCorrectly()
    {
        var price = new DealPrice(179m, 249m, "USD");
        price.DiscountPercentage.Should().BeApproximately(28.1m, 0.5m);
    }

    [Fact]
    public void DealPrice_WithNoOriginalPrice_ShouldReturnNullDiscount()
    {
        var price = new DealPrice(99m, null, "USD");
        price.DiscountPercentage.Should().BeNull();
        price.HasDiscount.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRaiseDealCreatedDomainEvent()
    {
        var deal = CreateValidDeal();
        var events = deal.GetDomainEvents();

        events.Should().ContainSingle()
            .Which.Should().BeOfType<DealCreatedDomainEvent>();
    }

    [Fact]
    public void Expire_ShouldRaiseDealExpiredDomainEvent()
    {
        var deal = CreateValidDeal();
        deal.ClearDomainEvents();

        deal.Expire(DealExpiredReason.Manual);

        deal.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<DealExpiredDomainEvent>();
    }
}
