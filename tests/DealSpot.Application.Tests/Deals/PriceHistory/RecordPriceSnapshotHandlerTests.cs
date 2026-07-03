using DealSpot.Application.Deals.PriceHistory;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.PriceHistory;

public sealed class RecordPriceSnapshotHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IPriceSnapshotRepository _snapshotRepo = Substitute.For<IPriceSnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RecordPriceSnapshotHandler _handler;

    public RecordPriceSnapshotHandlerTests()
    {
        _handler = new RecordPriceSnapshotHandler(
            _dealRepo, _snapshotRepo, _unitOfWork,
            Substitute.For<ILogger<RecordPriceSnapshotHandler>>());
    }

    [Fact]
    public async Task Handle_DealCreated_RecordsInitialSnapshot()
    {
        var price = new DealPrice(49.99m, 99.99m, "USD");
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test", "test-slug", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, "Amazon").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        await _handler.Handle(new DealCreatedDomainEvent(deal.Id, deal.TenantId), CancellationToken.None);

        _snapshotRepo.Received(1).Add(Arg.Is<PriceSnapshot>(s =>
            s.DealId == deal.Id && s.Price == 49.99m && s.Source == "initial"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PriceChanged_RecordsSnapshot()
    {
        var evt = new DealPriceChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), 49.99m, 29.99m, 99.99m, "USD");

        await _handler.Handle(evt, CancellationToken.None);

        _snapshotRepo.Received(1).Add(Arg.Is<PriceSnapshot>(s =>
            s.DealId == evt.DealId && s.Price == 29.99m && s.Source == "admin_update"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
