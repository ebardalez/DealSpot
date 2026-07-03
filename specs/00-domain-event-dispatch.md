# 00 — Domain Event Dispatch (Foundation)

**Phase:** Foundation · **Status:** Not built · **Depends on:** —

## Goal
Deals raise `DealCreatedDomainEvent` / `DealExpiredDomainEvent`, but `ApplicationDbContext.SaveChangesAsync` only calls `base`. Events are never published, so nothing reacts to them. Dispatch domain events after save so email alerts (03), cache invalidation (09), and score recalculation (05) can subscribe.

## Approach
Publish events via MediatR `INotification` after a successful save. Domain events must implement `INotification` (they already flow through `IDomainEvent` — make `IDomainEvent : INotification`, MediatR contracts is already referenced by Domain).

## Changes

### Domain
- `IDomainEvent : MediatR.INotification` (single line; package already present).

### Infrastructure
- In `ApplicationDbContext.SaveChangesAsync`:
  1. `var result = await base.SaveChangesAsync(ct);`
  2. Collect entities with domain events from `ChangeTracker`, copy events, `ClearDomainEvents()`.
  3. `foreach (e in events) await publisher.Publish(e, ct);`
  4. `return result;`
- Inject `IPublisher` (MediatR) into the context ctor.
- Dispatch **after** save (events see persisted state). Accept at-most-once delivery for MVP.

> `// ponytail:` dispatch inline after SaveChanges, no outbox. Add the Outbox pattern (Phase 3, see spec 12/roadmap) only when a handler must survive process crash.

## Acceptance
- Creating a deal invokes a test `INotificationHandler<DealCreatedDomainEvent>`.
- Events are cleared post-dispatch (no double fire on a second save).
- Existing `CreateDeal`/`ReportDealExpired` tests still pass.

## Test
`INotificationHandler` spy asserts one publish per created deal.
