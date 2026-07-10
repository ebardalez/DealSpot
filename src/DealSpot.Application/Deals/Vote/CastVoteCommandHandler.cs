using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

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
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CastVoteCommandHandler> logger) : ICommandHandler<CastVoteCommand, CastVoteResponse>
{
    public async Task<Result<CastVoteResponse>> Handle(CastVoteCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var deal = await dealRepository.GetByIdAsync(command.DealId, cancellationToken);
        if (deal is null)
            return Result.Failure<CastVoteResponse>(DealErrors.NotFound);

        var existing = await dealVoteRepository.GetAsync(command.DealId, userId, cancellationToken);
        int repDelta = 0;

        if (existing is null)
        {
            // New vote
            var vote = DealVote.Create(command.DealId, userId, command.Value);
            dealVoteRepository.Add(vote);
            deal.ApplyVote(command.Value);
            repDelta = command.Value; // +1 or -1
        }
        else if (existing.Value == command.Value)
        {
            // Same vote — no-op
            return new CastVoteResponse(deal.UpVoteCount, deal.DownVoteCount, deal.NetScore, existing.Value);
        }
        else
        {
            // Flip: remove old, apply new → net delta is 2× the new value
            deal.RemoveVote(existing.Value);
            existing.Flip();
            dealVoteRepository.Update(existing);
            deal.ApplyVote(existing.Value);
            repDelta = existing.Value * 2; // +2 or -2
        }

        dealRepository.Update(deal);

        // ponytail: rep change inline, skip self-votes
        if (repDelta != 0 && deal.SubmittedByUserId is not null && deal.SubmittedByUserId.Value != userId)
        {
            try
            {
                var submitter = await userRepository.GetByIdAsync(deal.SubmittedByUserId.Value, cancellationToken);
                if (submitter is not null)
                {
                    submitter.AddReputation(repDelta);
                    userRepository.Update(submitter);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update reputation on vote for deal {DealId}", command.DealId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CastVoteResponse(deal.UpVoteCount, deal.DownVoteCount, deal.NetScore, command.Value);
    }
}
