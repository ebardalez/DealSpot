using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.Vote;

public sealed record GetDealVoteQuery(Guid DealId) : IQuery<DealVoteDto>;

public sealed record DealVoteDto(int UpVoteCount, int DownVoteCount, int NetScore, int? UserVote);

public sealed class GetDealVoteQueryHandler(
    IDealRepository dealRepository,
    IDealVoteRepository dealVoteRepository,
    ICurrentUser currentUser) : IQueryHandler<GetDealVoteQuery, DealVoteDto>
{
    public async Task<Result<DealVoteDto>> Handle(GetDealVoteQuery query, CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetByIdAsync(query.DealId, cancellationToken);
        if (deal is null)
            return Result.Failure<DealVoteDto>(DealErrors.NotFound);

        int? userVote = null;
        if (currentUser.UserId.HasValue)
        {
            var vote = await dealVoteRepository.GetAsync(query.DealId, currentUser.UserId.Value, cancellationToken);
            userVote = vote?.Value;
        }

        return new DealVoteDto(deal.UpVoteCount, deal.DownVoteCount, deal.NetScore, userVote);
    }
}
