using DealSpot.Application.Abstractions.Caching;
using Microsoft.AspNetCore.OutputCaching;

namespace DealSpot.Infrastructure.Caching;

public sealed class OutputCacheInvalidator(IOutputCacheStore store) : ICacheInvalidator
{
    public async Task EvictAsync(string tag, CancellationToken cancellationToken = default) =>
        await store.EvictByTagAsync(tag, cancellationToken);
}
