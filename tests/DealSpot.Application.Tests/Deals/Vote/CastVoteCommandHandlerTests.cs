using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Deals.Vote;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Vote;

public sealed class CastVoteCommandHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IDealVoteRepository _voteRepo = Substitute.For<IDealVoteRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CastVoteCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DealId = Guid.NewGuid();

    public CastVoteCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _handler = new CastVoteCommandHandler(_dealRepo, _voteRepo, _currentUser, _unitOfWork);
    }

    private Deal CreateTestDeal()
    {
        var price = new DealPrice(29.99m, 49.99m, "USD");
        var result = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Test Deal", "test-deal-30", "A test deal", price,
            "https://example.com", "https://example.com?tag=test",
            null, "TestRetailer");
        return result.Value;
    }

    [Fact]
    public async Task Handle_NewUpvote_IncrementsUpVoteCount()
    {
        var deal = CreateTestDeal();
        _dealRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(deal);
        _voteRepo.GetAsync(Arg.Any<Guid>(), UserId, Arg.Any<CancellationToken>()).Returns((DealVote?)null);

        var result = await _handler.Handle(new CastVoteCommand(deal.Id, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpVoteCount.Should().Be(1);
        result.Value.DownVoteCount.Should().Be(0);
        result.Value.NetScore.Should().Be(1);
        result.Value.UserVote.Should().Be(1);
        _voteRepo.Received(1).Add(Arg.Any<DealVote>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateVote_IsNoOp()
    {
        var deal = CreateTestDeal();
        deal.ApplyVote(1); // simulate existing upvote
        var existingVote = DealVote.Create(deal.Id, UserId, 1);
        _dealRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(deal);
        _voteRepo.GetAsync(Arg.Any<Guid>(), UserId, Arg.Any<CancellationToken>()).Returns(existingVote);

        var result = await _handler.Handle(new CastVoteCommand(deal.Id, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpVoteCount.Should().Be(1);
        result.Value.NetScore.Should().Be(1);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FlipUpToDown_AdjustsByTwo()
    {
        var deal = CreateTestDeal();
        deal.ApplyVote(1); // existing upvote
        var existingVote = DealVote.Create(deal.Id, UserId, 1);
        _dealRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(deal);
        _voteRepo.GetAsync(Arg.Any<Guid>(), UserId, Arg.Any<CancellationToken>()).Returns(existingVote);

        var result = await _handler.Handle(new CastVoteCommand(deal.Id, -1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpVoteCount.Should().Be(0);
        result.Value.DownVoteCount.Should().Be(1);
        result.Value.NetScore.Should().Be(-1); // went from +1 to -1 = change of 2
        _voteRepo.Received(1).Update(Arg.Any<DealVote>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DealNotFound_ReturnsFailure()
    {
        _dealRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Deal?)null);

        var result = await _handler.Handle(new CastVoteCommand(DealId, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DealErrors.NotFound);
    }
}
