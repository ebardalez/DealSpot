using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;
using FluentValidation;

namespace DealSpot.Application.Watchlist.AddKeywordWatch;

public sealed record AddKeywordWatchCommand(string Keyword, decimal? TargetPrice) : ICommand<Guid>;

public sealed class AddKeywordWatchCommandValidator : AbstractValidator<AddKeywordWatchCommand>
{
    public AddKeywordWatchCommandValidator()
    {
        RuleFor(x => x.Keyword).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetPrice).GreaterThan(0).When(x => x.TargetPrice.HasValue);
    }
}

public sealed class AddKeywordWatchCommandHandler(
    IWatchlistRepository watchlistRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<AddKeywordWatchCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddKeywordWatchCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        if (await watchlistRepository.ExistsAsync(userId, tenantId, null, command.Keyword, cancellationToken))
            return Result.Failure<Guid>(WatchlistErrors.Duplicate);

        var entry = WatchlistEntry.CreateKeywordWatch(userId, tenantId, command.Keyword, command.TargetPrice);
        watchlistRepository.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }
}
