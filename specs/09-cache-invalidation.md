# 09 — Output Cache Invalidation

**Phase:** 2 · **Status:** Not built (tags declared, never evicted) · **Depends on:** 00

## Goal
`DealPage`/`CategoryPage` policies declare cache tags (`deals`, `categories`) but nothing calls `EvictByTagAsync`, so a new or expired deal stays hidden until TTL (up to 30 min). Evict on change.

## Changes

### Application
- `INotificationHandler<DealCreatedDomainEvent>` and `INotificationHandler<DealExpiredDomainEvent>` → evict tags.
- Cache is a web concern; expose via `Abstractions/Caching/ICacheInvalidator { Task EvictAsync(string tag, CancellationToken) }` so Application stays framework-free.

### Infrastructure / Web
- `OutputCacheInvalidator : ICacheInvalidator` wrapping `IOutputCacheStore.EvictByTagAsync`. Register in DI.
- Handlers evict `deals` (and `categories` when a category's deal set changes).

> Reuses dispatch from 00. If 03 already handles `DealCreatedDomainEvent`, add a second handler — MediatR runs all handlers for a notification.

## Acceptance
- Creating/expiring a deal evicts `deals`; next request re-renders fresh (new deal visible / expired deal gone) before TTL.
- Eviction failure doesn't fail the deal write (log + continue).

## Test
Spy `ICacheInvalidator`: `DealCreatedDomainEvent` triggers one `EvictAsync("deals")`.

> `// ponytail:` tag eviction only — no per-slug surgical invalidation. Add finer keys only if eviction proves too broad under load.
