using DealSpot.Domain.Deals;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DealSpot.Infrastructure.BackgroundJobs;

public sealed class DealUrlHealthCheckJob(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork,
    IHttpClientFactory httpClientFactory,
    ILogger<DealUrlHealthCheckJob> logger)
{
    private const int BatchSize = 100;

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting deal URL health check");
        var deals = await dealRepository.GetActiveDealsForUrlCheckAsync(BatchSize, cancellationToken);
        var client = httpClientFactory.CreateClient("DealHealthCheck");
        var expiredCount = 0;

        foreach (var deal in deals)
        {
            try
            {
                if (await IsUrlDeadAsync(client, deal.OriginalUrl, cancellationToken))
                {
                    deal.Expire(DealExpiredReason.UrlDead);
                    dealRepository.Update(deal);
                    expiredCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check URL for deal {DealId}", deal.Id);
            }
        }

        if (expiredCount > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Health check complete. Checked: {Checked}, Expired: {Expired}", deals.Count, expiredCount);
    }

    private static async Task<bool> IsUrlDeadAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, cancellationToken);
            return (int)response.StatusCode == 404 || (int)response.StatusCode >= 500;
        }
        catch (HttpRequestException) { return true; }
        catch (TaskCanceledException) { return false; }
    }
}
