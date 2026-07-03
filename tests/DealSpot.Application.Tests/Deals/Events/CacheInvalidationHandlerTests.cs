using DealSpot.Application.Abstractions.Caching;
using DealSpot.Application.Deals.Events;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Events;

public sealed class CacheInvalidationHandlerTests
{
    private readonly ICacheInvalidator _cacheInvalidator = Substitute.For<ICacheInvalidator>();

    [Fact]
    public async Task DealCreated_EvictsDealsAndCategoriesTags()
    {
        var handler = new DealCreatedCacheInvalidationHandler(
            _cacheInvalidator,
            Substitute.For<ILogger<DealCreatedCacheInvalidationHandler>>());

        var evt = new DealCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(evt, CancellationToken.None);

        await _cacheInvalidator.Received(1).EvictAsync("deals", Arg.Any<CancellationToken>());
        await _cacheInvalidator.Received(1).EvictAsync("categories", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DealExpired_EvictsDealsAndCategoriesTags()
    {
        var handler = new DealExpiredCacheInvalidationHandler(
            _cacheInvalidator,
            Substitute.For<ILogger<DealExpiredCacheInvalidationHandler>>());

        var evt = new DealExpiredDomainEvent(Guid.NewGuid(), Guid.NewGuid(), DealExpiredReason.Manual);

        await handler.Handle(evt, CancellationToken.None);

        await _cacheInvalidator.Received(1).EvictAsync("deals", Arg.Any<CancellationToken>());
        await _cacheInvalidator.Received(1).EvictAsync("categories", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DealCreated_EvictionFailure_DoesNotThrow()
    {
        _cacheInvalidator.EvictAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Redis down"));

        var handler = new DealCreatedCacheInvalidationHandler(
            _cacheInvalidator,
            Substitute.For<ILogger<DealCreatedCacheInvalidationHandler>>());

        var evt = new DealCreatedDomainEvent(Guid.NewGuid(), Guid.NewGuid());

        // Should not throw — eviction failure is swallowed
        await handler.Handle(evt, CancellationToken.None);
    }
}
