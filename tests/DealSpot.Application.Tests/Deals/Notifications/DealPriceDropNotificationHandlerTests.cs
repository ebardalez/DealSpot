using DealSpot.Application.Abstractions.Email;
using DealSpot.Application.Abstractions.Push;
using DealSpot.Application.Deals.Notifications;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Notifications;

public sealed class DealPriceDropNotificationHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly ICategoryRepository _categoryRepo = Substitute.For<ICategoryRepository>();
    private readonly ITenantRepository _tenantRepo = Substitute.For<ITenantRepository>();
    private readonly IWatchlistRepository _watchlistRepo = Substitute.For<IWatchlistRepository>();
    private readonly INotificationPreferenceRepository _prefRepo = Substitute.For<INotificationPreferenceRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPendingAlertRepository _pendingAlertRepo = Substitute.For<IPendingAlertRepository>();
    private readonly IPushSubscriptionRepository _pushSubRepo = Substitute.For<IPushSubscriptionRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IPushSender _pushSender = Substitute.For<IPushSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DealPriceDropNotificationHandler _handler;

    public DealPriceDropNotificationHandlerTests()
    {
        _handler = new DealPriceDropNotificationHandler(
            _dealRepo, _categoryRepo, _tenantRepo, _watchlistRepo,
            _prefRepo, _userRepo, _pendingAlertRepo, _pushSubRepo,
            _emailService, _pushSender, _unitOfWork,
            Substitute.For<ILogger<DealPriceDropNotificationHandler>>());
    }

    [Fact]
    public async Task Handle_PriceIncrease_DoesNothing()
    {
        var evt = new DealPriceChangedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), 29.99m, 49.99m, null, "USD");

        await _handler.Handle(evt, CancellationToken.None);

        // No repo calls — early return on price increase
        await _dealRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PriceDrop_AlertsCategoryWatcher()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dealId = Guid.NewGuid();

        var price = new DealPrice(29.99m, 99.99m, "USD");
        var deal = Deal.Create(tenantId, categoryId, null,
            "GPU Sale", "gpu-sale", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, "Amazon").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _categoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create(tenantId, "Tech", "tech").Value);
        _tenantRepo.GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Tenant.Create("techdeals", "TechDeals", "tech.deals.com", new TenantBranding("#000", "#fff", "", "", "", "")).Value);

        var watch = WatchlistEntry.CreateCategoryWatch(userId, tenantId, categoryId);
        _watchlistRepo.GetCategoryWatchersAsync(tenantId, categoryId, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry> { watch });
        _watchlistRepo.GetKeywordWatchersAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        _prefRepo.GetAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns((UserNotificationPreference?)null);

        var user = User.Create("watcher@test.com", "Watcher").Value;
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var evt = new DealPriceChangedDomainEvent(deal.Id, tenantId, 49.99m, 29.99m, 99.99m, "USD");
        await _handler.Handle(evt, CancellationToken.None);

        await _emailService.Received(1).SendDealAlertAsync(
            user.Email, user.DisplayName, Arg.Any<DealAlertEmailModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PriceDrop_KeywordWatcher_TargetPriceMet_GetsAlert()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var price = new DealPrice(19.99m, 49.99m, "USD");
        var deal = Deal.Create(tenantId, categoryId, null,
            "Fish Food Special", "fish-food", "desc", price,
            "https://example.com", "https://example.com?tag=x",
            null, "Chewy").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _categoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create(tenantId, "Aquarium", "aquarium").Value);
        _tenantRepo.GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Tenant.Create("reefdeals", "ReefDeals", "reef.deals.com", new TenantBranding("#000", "#fff", "", "", "", "")).Value);

        _watchlistRepo.GetCategoryWatchersAsync(tenantId, categoryId, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        // Keyword watcher with target price of 25
        var kwWatch = WatchlistEntry.CreateKeywordWatch(userId, tenantId, "Fish Food", 25m);
        _watchlistRepo.GetKeywordWatchersAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry> { kwWatch });

        _prefRepo.GetAsync(userId, tenantId, Arg.Any<CancellationToken>()).Returns((UserNotificationPreference?)null);

        var user = User.Create("fish@test.com", "FishLover").Value;
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Price dropped from 29.99 to 19.99, target was 25 — should alert
        var evt = new DealPriceChangedDomainEvent(deal.Id, tenantId, 29.99m, 19.99m, 49.99m, "USD");
        await _handler.Handle(evt, CancellationToken.None);

        await _emailService.Received(1).SendDealAlertAsync(
            user.Email, user.DisplayName, Arg.Any<DealAlertEmailModel>(), Arg.Any<CancellationToken>());
    }
}
