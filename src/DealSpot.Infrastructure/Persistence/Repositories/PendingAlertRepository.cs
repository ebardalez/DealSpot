using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class PendingAlertRepository(ApplicationDbContext dbContext) : IPendingAlertRepository
{
    public async Task<IReadOnlyList<PendingAlert>> GetUnsentAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PendingAlerts
            .Where(a => !a.IsSent)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(PendingAlert alert) => dbContext.PendingAlerts.Add(alert);
    public void Update(PendingAlert alert) => dbContext.PendingAlerts.Update(alert);
}
