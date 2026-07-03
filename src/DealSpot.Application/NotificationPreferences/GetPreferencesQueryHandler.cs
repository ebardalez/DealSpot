using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.NotificationPreferences;

public sealed record GetPreferencesQuery : IQuery<NotificationPreferencesDto>;

public sealed record NotificationPreferencesDto(
    bool NewDealInCategory, bool WatchlistMatch, bool DealExpiringSoon,
    NotificationFrequency Frequency);

public sealed class GetPreferencesQueryHandler(
    INotificationPreferenceRepository prefRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : IQueryHandler<GetPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(GetPreferencesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        var pref = await prefRepository.GetAsync(userId, tenantId, cancellationToken);

        // Return defaults if no prefs exist yet
        return new NotificationPreferencesDto(
            pref?.NewDealInCategory ?? true,
            pref?.WatchlistMatch ?? true,
            pref?.DealExpiringSoon ?? false,
            pref?.Frequency ?? NotificationFrequency.Instant);
    }
}
