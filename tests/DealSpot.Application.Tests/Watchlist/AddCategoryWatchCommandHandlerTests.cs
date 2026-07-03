using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Application.Watchlist.AddCategoryWatch;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Watchlist;

public sealed class AddCategoryWatchCommandHandlerTests
{
    private readonly IWatchlistRepository _watchlistRepo = Substitute.For<IWatchlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly AddCategoryWatchCommandHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private readonly Guid _tenantId;

    public AddCategoryWatchCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        var branding = new TenantBranding("#000", "#fff", "/logo.png", "/fav.ico", "desc", "keys");
        var tenant = Tenant.Create("test", "Test", "test.com", branding).Value;
        _tenantId = tenant.Id;
        _tenantContext.Current.Returns(tenant);
        _handler = new AddCategoryWatchCommandHandler(_watchlistRepo, _tenantContext, _currentUser, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NewWatch_Succeeds()
    {
        _watchlistRepo.ExistsAsync(UserId, _tenantId, CategoryId, null, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new AddCategoryWatchCommand(CategoryId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _watchlistRepo.Received(1).Add(Arg.Any<WatchlistEntry>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Duplicate_ReturnsFailure()
    {
        _watchlistRepo.ExistsAsync(UserId, _tenantId, CategoryId, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new AddCategoryWatchCommand(CategoryId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WatchlistErrors.Duplicate);
        _watchlistRepo.DidNotReceive().Add(Arg.Any<WatchlistEntry>());
    }
}
