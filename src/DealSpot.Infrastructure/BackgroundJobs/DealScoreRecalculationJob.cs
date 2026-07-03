using DealSpot.Application.Deals.Score;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DealSpot.Infrastructure.BackgroundJobs;

// ponytail: recalculates all active deals. Fine for <1000 deals. Add stale-only filter if perf matters.
public sealed class DealScoreRecalculationJob(
    IDealRepository dealRepository,
    IUnitOfWork unitOfWork,
    ILogger<DealScoreRecalculationJob> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting deal score recalculation");

        var deals = await dealRepository.GetActiveDealsForUrlCheckAsync(500, cancellationToken);
        var updated = 0;

        foreach (var deal in deals)
        {
            var newScore = DealScoreCalculator.Calculate(deal);

            if (newScore.Value != deal.DealScore.Value)
            {
                deal.UpdateDealScore(newScore);
                dealRepository.Update(deal);
                updated++;
            }
        }

        if (updated > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Score recalculation complete. Checked: {Checked}, Updated: {Updated}", deals.Count, updated);
    }
}
