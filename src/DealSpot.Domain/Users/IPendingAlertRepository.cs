namespace DealSpot.Domain.Users;

public interface IPendingAlertRepository
{
    Task<IReadOnlyList<PendingAlert>> GetUnsentAsync(CancellationToken cancellationToken = default);
    void Add(PendingAlert alert);
    void Update(PendingAlert alert);
}
