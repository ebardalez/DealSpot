using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Watchlist.RemoveWatch;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Watchlist;

public sealed class RemoveWatchCommandHandlerTests
{
    private readonly IWatchlistRepository _watchlistRepo = Substitute.For<IWatchlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveWatchCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();

    public RemoveWatchCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _handler = new RemoveWatchCommandHandler(_watchlistRepo, _currentUser, _unitOfWork);
    }

    [Fact]
    public async Task Handle_OwnEntry_RemovesSuccessfully()
    {
        var entry = WatchlistEntry.CreateCategoryWatch(UserId, Guid.NewGuid(), Guid.NewGuid());

        _watchlistRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _handler.Handle(new RemoveWatchCommand(entry.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _watchlistRepo.Received(1).Remove(entry);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsFailure()
    {
        var otherUserId = Guid.NewGuid();
        var entry = WatchlistEntry.CreateCategoryWatch(otherUserId, Guid.NewGuid(), Guid.NewGuid());

        _watchlistRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _handler.Handle(new RemoveWatchCommand(entry.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WatchlistErrors.NotOwner);
        _watchlistRepo.DidNotReceive().Remove(Arg.Any<WatchlistEntry>());
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        _watchlistRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WatchlistEntry?)null);

        var result = await _handler.Handle(new RemoveWatchCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WatchlistErrors.NotFound);
    }
}
