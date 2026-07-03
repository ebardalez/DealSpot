using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;
using FluentValidation;

namespace DealSpot.Application.Watchlist.AddCategoryWatch;

public sealed record AddCategoryWatchCommand(Guid CategoryId) : ICommand<Guid>;

public sealed class AddCategoryWatchCommandValidator : AbstractValidator<AddCategoryWatchCommand>
{
    public AddCategoryWatchCommandValidator() => RuleFor(x => x.CategoryId).NotEmpty();
}

public sealed class AddCategoryWatchCommandHandler(
    IWatchlistRepository watchlistRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<AddCategoryWatchCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCategoryWatchCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        if (await watchlistRepository.ExistsAsync(userId, tenantId, command.CategoryId, null, cancellationToken))
            return Result.Failure<Guid>(WatchlistErrors.Duplicate);

        var entry = WatchlistEntry.CreateCategoryWatch(userId, tenantId, command.CategoryId);
        watchlistRepository.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }
}
