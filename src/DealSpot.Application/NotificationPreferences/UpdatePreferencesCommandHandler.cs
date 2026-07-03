using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.NotificationPreferences;

public sealed record UpdatePreferencesCommand(
    bool NewDealInCategory, bool WatchlistMatch, bool DealExpiringSoon,
    NotificationFrequency Frequency) : ICommand;

public sealed class UpdatePreferencesCommandHandler(
    INotificationPreferenceRepository prefRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdatePreferencesCommand>
{
    public async Task<Result> Handle(UpdatePreferencesCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        var pref = await prefRepository.GetAsync(userId, tenantId, cancellationToken);

        if (pref is null)
        {
            pref = UserNotificationPreference.Create(userId, tenantId);
            pref.Update(command.NewDealInCategory, command.WatchlistMatch, command.DealExpiringSoon, command.Frequency);
            prefRepository.Add(pref);
        }
        else
        {
            pref.Update(command.NewDealInCategory, command.WatchlistMatch, command.DealExpiringSoon, command.Frequency);
            prefRepository.Update(pref);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
