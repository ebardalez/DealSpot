using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using FluentValidation;

namespace DealSpot.Application.Deals.Vote;

public sealed record CastVoteCommand(Guid DealId, int Value) : ICommand<CastVoteResponse>;

public sealed record CastVoteResponse(int UpVoteCount, int DownVoteCount, int NetScore, int UserVote);

public sealed class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    public CastVoteCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.Value).Must(v => v is 1 or -1).WithMessage("Vote must be +1 or -1.");
    }
}

public sealed class CastVoteCommandHandler(
    IDealRepository dealRepository,
    IDealVoteRepository dealVoteRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<CastVoteCommand, CastVoteResponse>
{
    public async Task<Result<CastVoteResponse>> Handle(CastVoteCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null)
            return Result.Failure<CastVoteResponse>(DealErrors.NotFound);

        var existing = await dealVoteRepository.GetAsync(command.DealId, userId, cancellationToken);

        if (existing is null)
        {
            // New vote
            var vote = DealVote.Create(command.DealId, userId, command.Value);
            dealVoteRepository.Add(vote);
            deal.ApplyVote(command.Value);
        }
        else if (existing.Value == command.Value)
        {
            // Same vote — no-op
            return new CastVoteResponse(deal.UpVoteCount, deal.DownVoteCount, deal.NetScore, existing.Value);
        }
        else
        {
            // Flip: remove old, apply new
            deal.RemoveVote(existing.Value);
            existing.Flip();
            dealVoteRepository.Update(existing);
            deal.ApplyVote(existing.Value);
        }

        dealRepository.Update(deal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CastVoteResponse(deal.UpVoteCount, deal.DownVoteCount, deal.NetScore, command.Value);
    }
}
