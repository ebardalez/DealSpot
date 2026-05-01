using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Deals.Events;

public sealed record DealCreatedDomainEvent(Guid DealId, Guid TenantId) : IDomainEvent;

public sealed record DealExpiredDomainEvent(
    Guid DealId,
    Guid TenantId,
    DealExpiredReason Reason) : IDomainEvent;
