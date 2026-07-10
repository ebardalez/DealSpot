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
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Notifications;

public sealed class DealCreatedNotificationHandlerTests
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
    private readonly DealCreatedNotificationHandler _handler;

    private readonly Tenant _tenant;
    private readonly Category _category;
    private readonly Deal _deal;
    private readonly User _watcherUser;

    public DealCreatedNotificationHandlerTests()
    {
        var branding = new TenantBranding("#000", "#fff", "/logo.png", "/fav.ico", "desc", "keys");
        _tenant = Tenant.Create("tech", "TechDeals", "tech.deals.com", branding).Value;
        _category = Category.Create(_tenant.Id, "Laptops", "laptops").Value;

        var price = new DealPrice(799m, 999m, "USD");
        _deal = Deal.Create(
            _tenant.Id, _category.Id, null,
            "Gaming Laptop Sale", "gaming-laptop-sale-799", "Great deal", price,
            "https://store.com/laptop", "https://store.com/laptop?tag=ds",
            null, "TechStore").Value;

        _watcherUser = User.Create("watcher@example.com", "Watcher").Value;

        _dealRepo.GetByIdAsync(_deal.Id, Arg.Any<CancellationToken>()).Returns(_deal);
        _categoryRepo.GetByIdAsync(_category.Id, Arg.Any<CancellationToken>()).Returns(_category);
        _tenantRepo.GetByIdAsync(_tenant.Id, Arg.Any<CancellationToken>()).Returns(_tenant);
        _userRepo.GetByIdAsync(_watcherUser.Id, Arg.Any<CancellationToken>()).Returns(_watcherUser);

        _handler = new DealCreatedNotificationHandler(
            _dealRepo, _categoryRepo, _tenantRepo, _watchlistRepo,
            _prefRepo, _userRepo, _pendingAlertRepo, _pushSubRepo,
            _emailService, _pushSender, _unitOfWork,
            Substitute.For<ILogger<DealCreatedNotificationHandler>>());
    }

    [Fact]
    public async Task Handle_CategoryWatcher_InstantPref_SendsEmail()
    {
        var watch = WatchlistEntry.CreateCategoryWatch(_watcherUser.Id, _tenant.Id, _category.Id);
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns([watch]);
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());
        // No pref saved → defaults to instant + enabled
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns((UserNotificationPreference?)null);

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        await _emailService.Received(1).SendDealAlertAsync(
            _watcherUser.Email, _watcherUser.DisplayName,
            Arg.Any<DealAlertEmailModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_KeywordWatcher_MatchesTitle_SendsWatchlistEmail()
    {
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        var kwWatch = WatchlistEntry.CreateKeywordWatch(_watcherUser.Id, _tenant.Id, "Gaming Laptop");
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns([kwWatch]);
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns((UserNotificationPreference?)null);

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        await _emailService.Received(1).SendWatchlistMatchAsync(
            _watcherUser.Email, _watcherUser.DisplayName,
            Arg.Any<WatchlistMatchEmailModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DigestUser_EnqueuesAlert_NoImmediateEmail()
    {
        var watch = WatchlistEntry.CreateCategoryWatch(_watcherUser.Id, _tenant.Id, _category.Id);
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns([watch]);
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        var pref = UserNotificationPreference.Create(_watcherUser.Id, _tenant.Id);
        pref.Update(true, true, false, NotificationFrequency.DailyDigest);
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>()).Returns(pref);

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        _pendingAlertRepo.Received(1).Add(Arg.Any<PendingAlert>());
        await _emailService.DidNotReceive().SendDealAlertAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DealAlertEmailModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PrefDisabled_NoEmailNoEnqueue()
    {
        var watch = WatchlistEntry.CreateCategoryWatch(_watcherUser.Id, _tenant.Id, _category.Id);
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns([watch]);
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        var pref = UserNotificationPreference.Create(_watcherUser.Id, _tenant.Id);
        pref.Update(false, false, false, NotificationFrequency.Instant); // all off
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>()).Returns(pref);

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        await _emailService.DidNotReceive().SendDealAlertAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DealAlertEmailModel>(), Arg.Any<CancellationToken>());
        _pendingAlertRepo.DidNotReceive().Add(Arg.Any<PendingAlert>());
    }

    [Fact]
    public async Task Handle_PushEnabled_SendsPushNotification()
    {
        var watch = WatchlistEntry.CreateCategoryWatch(_watcherUser.Id, _tenant.Id, _category.Id);
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns([watch]);
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        var pref = UserNotificationPreference.Create(_watcherUser.Id, _tenant.Id);
        pref.Update(true, true, false, NotificationFrequency.Instant);
        pref.SetPushEnabled(true);
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>()).Returns(pref);

        var pushSub = PushSubscription.Create(_watcherUser.Id, _tenant.Id, "https://push.example.com/1", "key1", "auth1");
        _pushSubRepo.GetByUserAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PushSubscription> { pushSub });
        _pushSender.SendAsync(pushSub, Arg.Any<PushPayload>(), Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        await _pushSender.Received(1).SendAsync(pushSub, Arg.Any<PushPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PushGone_RemovesSubscription()
    {
        var watch = WatchlistEntry.CreateCategoryWatch(_watcherUser.Id, _tenant.Id, _category.Id);
        _watchlistRepo.GetCategoryWatchersAsync(_tenant.Id, _category.Id, Arg.Any<CancellationToken>())
            .Returns([watch]);
        _watchlistRepo.GetKeywordWatchersAsync(_tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchlistEntry>());

        var pref = UserNotificationPreference.Create(_watcherUser.Id, _tenant.Id);
        pref.Update(true, true, false, NotificationFrequency.Instant);
        pref.SetPushEnabled(true);
        _prefRepo.GetAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>()).Returns(pref);

        var pushSub = PushSubscription.Create(_watcherUser.Id, _tenant.Id, "https://push.example.com/expired", "key1", "auth1");
        _pushSubRepo.GetByUserAsync(_watcherUser.Id, _tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PushSubscription> { pushSub });
        _pushSender.SendAsync(pushSub, Arg.Any<PushPayload>(), Arg.Any<CancellationToken>()).Returns(false); // 410 Gone

        await _handler.Handle(new DealCreatedDomainEvent(_deal.Id, _tenant.Id), CancellationToken.None);

        _pushSubRepo.Received(1).Remove(pushSub);
    }
}
