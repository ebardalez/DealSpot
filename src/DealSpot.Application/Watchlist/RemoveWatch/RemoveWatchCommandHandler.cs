using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.Watchlist.RemoveWatch;

public sealed record RemoveWatchCommand(Guid WatchlistEntryId) : ICommand;

public sealed class RemoveWatchCommandHandler(
    IWatchlistRepository watchlistRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveWatchCommand>
{
    public async Task<Result> Handle(RemoveWatchCommand command, CancellationToken cancellationToken)
    {
        var entry = await watchlistRepository.GetByIdAsync(command.WatchlistEntryId, cancellationToken);
        if (entry is null) return Result.Failure(WatchlistErrors.NotFound);
        if (entry.UserId != currentUser.UserId!.Value) return Result.Failure(WatchlistErrors.NotOwner);

        watchlistRepository.Remove(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
