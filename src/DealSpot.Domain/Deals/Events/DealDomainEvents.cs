using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Deals.Events;

public sealed record DealCreatedDomainEvent(Guid DealId, Guid TenantId) : IDomainEvent;

public sealed record DealExpiredDomainEvent(
    Guid DealId,
    Guid TenantId,
    DealExpiredReason Reason) : IDomainEvent;

public sealed record DealPriceChangedDomainEvent(
    Guid DealId,
    Guid TenantId,
    decimal OldPrice,
    decimal NewPrice,
    decimal? NewOriginalPrice,
    string Currency) : IDomainEvent;
