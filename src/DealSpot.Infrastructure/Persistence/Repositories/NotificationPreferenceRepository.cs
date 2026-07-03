using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class NotificationPreferenceRepository(ApplicationDbContext dbContext) : INotificationPreferenceRepository
{
    public async Task<UserNotificationPreference?> GetAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId, cancellationToken);

    public void Add(UserNotificationPreference preference) => dbContext.NotificationPreferences.Add(preference);
    public void Update(UserNotificationPreference preference) => dbContext.NotificationPreferences.Update(preference);
}
