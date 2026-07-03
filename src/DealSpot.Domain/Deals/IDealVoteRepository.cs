namespace DealSpot.Domain.Deals;

public interface IDealVoteRepository
{
    Task<DealVote?> GetAsync(Guid dealId, Guid userId, CancellationToken cancellationToken = default);
    void Add(DealVote vote);
    void Update(DealVote vote);
}
