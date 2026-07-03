using DealSpot.Application.Deals.ApproveDeal;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals;

public sealed class ApproveDealCommandHandlerTests
{
    private readonly IDealRepository _dealRepository = Substitute.For<IDealRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ApproveDealCommandHandler _handler;

    public ApproveDealCommandHandlerTests()
    {
        _handler = new ApproveDealCommandHandler(_dealRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithPendingDeal_ShouldApproveAndSave()
    {
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Deal", "test-deal-10", "Desc",
            new DealPrice(10m, 20m, "USD"),
            "https://example.com", "https://example.com?tag=test",
            null, "Amazon",
            DealStatus.PendingApproval).Value;

        _dealRepository.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(new ApproveDealCommand(deal.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deal.Status.Should().Be(DealStatus.Active);
        _dealRepository.Received(1).Update(deal);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDealNotFound_ShouldReturnFailure()
    {
        _dealRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Deal?)null);

        var result = await _handler.Handle(new ApproveDealCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WhenDealAlreadyActive_ShouldReturnFailure()
    {
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Active Deal", "active-deal-10", "Desc",
            new DealPrice(10m, null, "USD"),
            "https://example.com", "https://example.com?tag=test",
            null, "Amazon").Value; // defaults to Active

        _dealRepository.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(new ApproveDealCommand(deal.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.NotPendingApproval);
    }
}
