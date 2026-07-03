using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence.Repositories;

public sealed class DealVoteRepository(ApplicationDbContext dbContext) : IDealVoteRepository
{
    public async Task<DealVote?> GetAsync(Guid dealId, Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.DealVotes
            .FirstOrDefaultAsync(v => v.DealId == dealId && v.UserId == userId, cancellationToken);

    public void Add(DealVote vote) => dbContext.DealVotes.Add(vote);
    public void Update(DealVote vote) => dbContext.DealVotes.Update(vote);
}
