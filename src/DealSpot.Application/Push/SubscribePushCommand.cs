using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.Push;

public sealed record SubscribePushCommand(string Endpoint, string P256dh, string Auth) : ICommand;

public sealed class SubscribePushCommandHandler(
    IPushSubscriptionRepository pushRepository,
    INotificationPreferenceRepository prefRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<SubscribePushCommand>
{
    public async Task<Result> Handle(SubscribePushCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        // Avoid duplicate endpoints
        var existing = await pushRepository.GetByEndpointAsync(command.Endpoint, cancellationToken);
        if (existing is not null) return Result.Success();

        var sub = PushSubscription.Create(userId, tenantId, command.Endpoint, command.P256dh, command.Auth);
        pushRepository.Add(sub);

        // Auto-enable push preference
        var pref = await prefRepository.GetAsync(userId, tenantId, cancellationToken);
        if (pref is null)
        {
            pref = UserNotificationPreference.Create(userId, tenantId);
            pref.SetPushEnabled(true);
            prefRepository.Add(pref);
        }
        else if (!pref.PushEnabled)
        {
            pref.SetPushEnabled(true);
            prefRepository.Update(pref);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
