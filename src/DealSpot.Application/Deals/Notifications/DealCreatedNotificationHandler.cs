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

/// <summary>
/// Matches new deals against watchlists and sends instant alerts or enqueues digest alerts.
/// </summary>
public sealed class DealCreatedNotificationHandler(
    IDealRepository dealRepository,
    ICategoryRepository categoryRepository,
    ITenantRepository tenantRepository,
    IWatchlistRepository watchlistRepository,
    INotificationPreferenceRepository prefRepository,
    IUserRepository userRepository,
    IPendingAlertRepository pendingAlertRepository,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    ILogger<DealCreatedNotificationHandler> logger) : INotificationHandler<DealCreatedDomainEvent>
{
    public async Task Handle(DealCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var deal = await dealRepository.GetByIdAsync(notification.DealId, cancellationToken);
            if (deal is null) return;

            var category = await categoryRepository.GetByIdAsync(deal.CategoryId, cancellationToken);
            var tenant = await tenantRepository.GetByIdAsync(deal.TenantId, cancellationToken);
            if (category is null || tenant is null) return;

            // Collect unique user IDs → alert type
            var userAlerts = new Dictionary<Guid, (string AlertType, string? Keyword)>();

            // 1. Category watchers
            var catWatchers = await watchlistRepository.GetCategoryWatchersAsync(deal.TenantId, deal.CategoryId, cancellationToken);
            foreach (var w in catWatchers)
                userAlerts.TryAdd(w.UserId, ("NewDealInCategory", null));

            // 2. Keyword watchers — match against deal title (case-insensitive)
            var kwWatchers = await watchlistRepository.GetKeywordWatchersAsync(deal.TenantId, cancellationToken);
            foreach (var w in kwWatchers.Where(w => deal.Title.Contains(w.ProductKeyword!, StringComparison.OrdinalIgnoreCase)))
                userAlerts.TryAdd(w.UserId, ("WatchlistMatch", w.ProductKeyword));

            // Don't alert the deal submitter
            if (deal.SubmittedByUserId.HasValue)
                userAlerts.Remove(deal.SubmittedByUserId.Value);

            foreach (var (userId, (alertType, keyword)) in userAlerts)
            {
                var pref = await prefRepository.GetAsync(userId, deal.TenantId, cancellationToken);

                // No prefs → use defaults (both on, instant)
                var isEnabled = alertType switch
                {
                    "NewDealInCategory" => pref?.NewDealInCategory ?? true,
                    "WatchlistMatch" => pref?.WatchlistMatch ?? true,
                    _ => false
                };

                if (!isEnabled) continue;

                var frequency = pref?.Frequency ?? NotificationFrequency.Instant;

                if (frequency == NotificationFrequency.DailyDigest)
                {
                    var alert = PendingAlert.Create(userId, deal.TenantId, deal.Id, alertType, keyword);
                    pendingAlertRepository.Add(alert);
                }
                else
                {
                    var user = await userRepository.GetByIdAsync(userId, cancellationToken);
                    if (user is null) continue;

                    var dealUrl = $"https://{tenant.Domain}/deals/{deal.Slug}";
                    var model = new DealAlertEmailModel(
                        tenant.Name, deal.Title, dealUrl, deal.ImageUrl ?? string.Empty,
                        deal.Price.CurrentPrice, deal.Price.OriginalPrice, deal.Price.Currency,
                        category.Name, deal.RetailerName);

                    if (alertType == "WatchlistMatch" && keyword is not null)
                    {
                        var watchModel = new WatchlistMatchEmailModel(tenant.Name, keyword, [model]);
                        await emailService.SendWatchlistMatchAsync(user.Email, user.DisplayName, watchModel, cancellationToken);
                    }
                    else
                    {
                        await emailService.SendDealAlertAsync(user.Email, user.DisplayName, model, cancellationToken);
                    }
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // ponytail: notification failure must not fail the deal write
            logger.LogError(ex, "Failed to process notifications for deal {DealId}", notification.DealId);
        }
    }
}
