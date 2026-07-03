using DealSpot.Application.Abstractions.Email;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using DealSpot.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace DealSpot.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily digest: groups pending alerts by user, sends one email per user, marks sent.
/// ponytail: single-query batch, no streaming. Scale only if > 10k alerts/day.
/// </summary>
public sealed class DailyDigestJob(
    IPendingAlertRepository pendingAlertRepository,
    IUserRepository userRepository,
    IDealRepository dealRepository,
    ICategoryRepository categoryRepository,
    ITenantRepository tenantRepository,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    ILogger<DailyDigestJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var unsent = await pendingAlertRepository.GetUnsentAsync(cancellationToken);
        if (unsent.Count == 0) return;

        var grouped = unsent.GroupBy(a => (a.UserId, a.TenantId));

        foreach (var group in grouped)
        {
            try
            {
                var (userId, tenantId) = group.Key;
                var user = await userRepository.GetByIdAsync(userId, cancellationToken);
                var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
                if (user is null || tenant is null) continue;

                var dealModels = new List<DealAlertEmailModel>();

                foreach (var alert in group)
                {
                    var deal = await dealRepository.GetByIdAsync(alert.DealId, cancellationToken);
                    if (deal is null) continue;

                    var category = await categoryRepository.GetByIdAsync(deal.CategoryId, cancellationToken);
                    var dealUrl = $"https://{tenant.Domain}/deals/{deal.Slug}";

                    dealModels.Add(new DealAlertEmailModel(
                        tenant.Name, deal.Title, dealUrl, deal.ImageUrl ?? string.Empty,
                        deal.Price.CurrentPrice, deal.Price.OriginalPrice, deal.Price.Currency,
                        category?.Name ?? string.Empty, deal.RetailerName));
                }

                if (dealModels.Count > 0)
                {
                    await emailService.SendDigestAsync(
                        user.Email, user.DisplayName,
                        new DigestEmailModel(tenant.Name, dealModels),
                        cancellationToken);
                }

                foreach (var alert in group)
                {
                    alert.MarkSent();
                    pendingAlertRepository.Update(alert);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Digest failed for user {UserId} tenant {TenantId}", group.Key.UserId, group.Key.TenantId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
