using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Deals;

public sealed class DealVote : Entity
{
    private DealVote() { }

    public Guid DealId { get; private set; }
    public Guid UserId { get; private set; }
    public int Value { get; private set; } // +1 or -1
    public DateTime CreatedAtUtc { get; private set; }

    public static DealVote Create(Guid dealId, Guid userId, int value) =>
        new()
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            UserId = userId,
            Value = value,
            CreatedAtUtc = DateTime.UtcNow
        };

    public void Flip()
    {
        Value = -Value;
    }
}
