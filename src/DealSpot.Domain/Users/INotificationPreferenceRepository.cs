namespace DealSpot.Domain.Users;

public interface INotificationPreferenceRepository
{
    Task<UserNotificationPreference?> GetAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    void Add(UserNotificationPreference preference);
    void Update(UserNotificationPreference preference);
}
