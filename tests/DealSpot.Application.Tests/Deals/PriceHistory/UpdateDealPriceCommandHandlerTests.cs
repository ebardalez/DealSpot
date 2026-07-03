using DealSpot.Application.Deals.PriceHistory;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.PriceHistory;

public sealed class UpdateDealPriceCommandHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateDealPriceCommandHandler _handler;

    public UpdateDealPriceCommandHandlerTests()
    {
        _handler = new UpdateDealPriceCommandHandler(_dealRepo, _unitOfWork);
    }

    private static Deal CreateDeal(decimal current = 49.99m, decimal? original = 99.99m)
    {
        var price = new DealPrice(current, original, "USD");
        return Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test Deal", "test-deal", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, "Amazon").Value;
    }

    [Fact]
    public async Task Handle_UpdatesPrice_RaisesDomainEvent()
    {
        var deal = CreateDeal();
        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(
            new UpdateDealPriceCommand(deal.Id, 29.99m, 99.99m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deal.Price.CurrentPrice.Should().Be(29.99m);
        deal.GetDomainEvents().Should().ContainSingle(e => e is DealSpot.Domain.Deals.Events.DealPriceChangedDomainEvent);
        _dealRepo.Received(1).Update(deal);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SamePrice_NoOp()
    {
        var deal = CreateDeal(49.99m, 99.99m);
        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(
            new UpdateDealPriceCommand(deal.Id, 49.99m, 99.99m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // DealCreatedDomainEvent from Create + no PriceChanged
        deal.GetDomainEvents().Should().NotContain(e => e is DealSpot.Domain.Deals.Events.DealPriceChangedDomainEvent);
    }

    [Fact]
    public async Task Handle_DealNotFound_ReturnsFailure()
    {
        _dealRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Deal?)null);

        var result = await _handler.Handle(
            new UpdateDealPriceCommand(Guid.NewGuid(), 29.99m, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.NotFound);
    }

    [Fact]
    public async Task Handle_InvalidPrice_ReturnsFailure()
    {
        var deal = CreateDeal();
        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(
            new UpdateDealPriceCommand(deal.Id, 0m, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.InvalidPrice);
    }
}
