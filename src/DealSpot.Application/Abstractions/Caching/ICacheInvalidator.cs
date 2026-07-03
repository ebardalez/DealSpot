namespace DealSpot.Application.Abstractions.Caching;

public interface ICacheInvalidator
{
    Task EvictAsync(string tag, CancellationToken cancellationToken = default);
}
