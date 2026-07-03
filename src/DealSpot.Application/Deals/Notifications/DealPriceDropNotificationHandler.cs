using DealSpot.Application.Abstractions.Email;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DealSpot.Application.Deals.Notifications;

// ponytail: reuses WatchlistMatch pref for price drops. Split to separate pref if users want distinct control.
public sealed class DealPriceDropNotificationHandler(
    IDealRepository dealRepository,
    ICategoryRepository categoryRepository,
    ITenantRepository tenantRepository,
    IWatchlistRepository watchlistRepository,
    INotificationPreferenceRepository prefRepository,
    IUserRepository userRepository,
    IPendingAlertRepository pendingAlertRepository,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    ILogger<DealPriceDropNotificationHandler> logger) : INotificationHandler<DealPriceChangedDomainEvent>
{
    public async Task Handle(DealPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Only alert on price drops
        if (notification.NewPrice >= notification.OldPrice)
            return;

        try
        {
            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal is null || deal.Status != DealStatus.Active) return;

            var category = await categoryRepository.GetByIdAsync(deal.CategoryId, cancellationToken);
            var tenant = await tenantRepository.GetByIdAsync(deal.TenantId, cancellationToken);
            if (category is null || tenant is null) return;

            var userAlerts = new Dictionary<Guid, string?>();

            // Category watchers
            var catWatchers = await watchlistRepository.GetCategoryWatchersAsync(deal.TenantId, deal.CategoryId, cancellationToken);
            foreach (var w in catWatchers)
                userAlerts.TryAdd(w.UserId, null);

            // Keyword watchers — match title + check target price
            var kwWatchers = await watchlistRepository.GetKeywordWatchersAsync(deal.TenantId, cancellationToken);
            foreach (var w in kwWatchers.Where(w =>
                deal.Title.Contains(w.ProductKeyword!, StringComparison.OrdinalIgnoreCase) &&
                (!w.TargetPrice.HasValue || notification.NewPrice <= w.TargetPrice.Value)))
            {
                userAlerts.TryAdd(w.UserId, w.ProductKeyword);
            }

            if (deal.SubmittedByUserId.HasValue)
                userAlerts.Remove(deal.SubmittedByUserId.Value);

            foreach (var (userId, keyword) in userAlerts)
            {
                var pref = await prefRepository.GetAsync(userId, deal.TenantId, cancellationToken);
                if (!(pref?.WatchlistMatch ?? true)) continue;

                var frequency = pref?.Frequency ?? NotificationFrequency.Instant;
                var alertType = "PriceDrop";

                if (frequency == NotificationFrequency.DailyDigest)
                {
                    pendingAlertRepository.Add(PendingAlert.Create(userId, deal.TenantId, deal.Id, alertType, keyword));
                }
                else
                {
                    var user = await userRepository.GetByIdAsync(userId, cancellationToken);
                    if (user is null) continue;

                    var dealUrl = $"https://{tenant.Domain}/deals/{deal.Slug}";
                    // ponytail: reuse DealAlertEmailModel. Add dedicated PriceDropEmailModel when email template diverges.
                    var model = new DealAlertEmailModel(
                        tenant.Name, $"Price Drop! {deal.Title}", dealUrl, deal.ImageUrl ?? string.Empty,
                        notification.NewPrice, notification.OldPrice, notification.Currency,
                        category.Name, deal.RetailerName);

                    await emailService.SendDealAlertAsync(user.Email, user.DisplayName, model, cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process price drop notifications for deal {DealId}", notification.DealId);
        }
    }
}
