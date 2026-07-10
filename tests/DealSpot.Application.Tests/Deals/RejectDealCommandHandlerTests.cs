using DealSpot.Application.Deals.RejectDeal;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using DealSpot.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals;

public sealed class RejectDealCommandHandlerTests
{
    private readonly IDealRepository _dealRepository = Substitute.For<IDealRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RejectDealCommandHandler _handler;

    public RejectDealCommandHandlerTests()
    {
        _handler = new RejectDealCommandHandler(
            _dealRepository, _userRepository, _unitOfWork,
            Substitute.For<ILogger<RejectDealCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WithPendingDeal_ShouldRejectAndSave()
    {
        var submitterId = Guid.NewGuid();
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), submitterId,
            "Test Deal", "test-deal-10", "Desc",
            new DealPrice(10m, 20m, "USD"),
            "https://example.com", "https://example.com?tag=test",
            null, "Amazon",
            DealStatus.PendingApproval).Value;

        var user = User.Create("test@example.com", "Test").Value;
        user.AddReputation(20); // start with some rep

        _dealRepository.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _userRepository.GetByIdAsync(submitterId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new RejectDealCommand(deal.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deal.Status.Should().Be(DealStatus.Rejected);
        user.ReputationScore.Should().Be(18); // 20 - 2
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RejectClampsReputationAtZero()
    {
        var submitterId = Guid.NewGuid();
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), submitterId,
            "Test Deal", "test-deal-10", "Desc",
            new DealPrice(10m, 20m, "USD"),
            "https://example.com", "https://example.com?tag=test",
            null, "Amazon",
            DealStatus.PendingApproval).Value;

        var user = User.Create("test@example.com", "Test").Value; // rep = 0

        _dealRepository.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _userRepository.GetByIdAsync(submitterId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new RejectDealCommand(deal.Id), CancellationToken.None);

        user.ReputationScore.Should().Be(0); // clamped at 0
    }

    [Fact]
    public async Task Handle_WhenDealNotFound_ShouldReturnFailure()
    {
        _dealRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Deal?)null);

        var result = await _handler.Handle(new RejectDealCommand(Guid.NewGuid()), CancellationToken.None);

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
            null, "Amazon").Value;

        _dealRepository.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        var result = await _handler.Handle(new RejectDealCommand(deal.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.NotPendingApproval);
    }
}
