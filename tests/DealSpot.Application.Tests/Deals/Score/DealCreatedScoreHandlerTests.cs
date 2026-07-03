using DealSpot.Application.Deals.Score;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Score;

public sealed class DealCreatedScoreHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DealCreatedScoreHandler _handler;

    public DealCreatedScoreHandlerTests()
    {
        _handler = new DealCreatedScoreHandler(
            _dealRepo, _unitOfWork,
            Substitute.For<ILogger<DealCreatedScoreHandler>>());
    }

    [Fact]
    public async Task Handle_CalculatesAndPersistsScore()
    {
        var price = new DealPrice(29.99m, 59.99m, "USD");
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test", "test-slug", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, "Amazon").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        await _handler.Handle(new DealCreatedDomainEvent(deal.Id, deal.TenantId), CancellationToken.None);

        deal.DealScore.IsCalculated.Should().BeTrue();
        deal.DealScore.Value.Should().BeGreaterThan(0);
        _dealRepo.Received(1).Update(deal);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
