using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.Push;

public sealed record UnsubscribePushCommand(string Endpoint) : ICommand;

public sealed class UnsubscribePushCommandHandler(
    IPushSubscriptionRepository pushRepository,
    INotificationPreferenceRepository prefRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UnsubscribePushCommand>
{
    public async Task<Result> Handle(UnsubscribePushCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        var existing = await pushRepository.GetByEndpointAsync(command.Endpoint, cancellationToken);
        if (existing is not null)
            pushRepository.Remove(existing);

        // If no more subscriptions, disable push pref
        var remaining = await pushRepository.GetByUserAsync(userId, tenantId, cancellationToken);
        // remaining still includes the one we just removed (not saved yet), so check count <= 1
        if (remaining.Count <= 1)
        {
            var pref = await prefRepository.GetAsync(userId, tenantId, cancellationToken);
            if (pref?.PushEnabled == true)
            {
                pref.SetPushEnabled(false);
                prefRepository.Update(pref);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
