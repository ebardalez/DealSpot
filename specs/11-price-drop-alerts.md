# 11 — Price Drop Alerts

**Phase:** 3 · **Status:** ✅ Done · **Depends on:** 02, 03, 10

## Goal
Email users when a watched deal's price drops, especially when it hits their `TargetPrice`.

## What Was Built

### Application
- `DealPriceDropNotificationHandler` (`INotificationHandler<DealPriceChangedDomainEvent>`):
  1. Early-returns on price increases (`NewPrice >= OldPrice`).
  2. Loads deal, category, tenant.
  3. Matches category watchers for the deal's category.
  4. Matches keyword watchers where deal title contains keyword AND (`TargetPrice` is null OR `NewPrice <= TargetPrice`).
  5. Excludes the deal submitter.
  6. Respects `WatchlistMatch` notification preference + frequency (instant vs digest).
  7. Instant: reuses `SendDealAlertAsync` with "Price Drop!" prefix in title.
  8. Digest: enqueues `PendingAlert` with `AlertType = "PriceDrop"`.

## Deviations from Original Spec
- **No cooldown**: `// ponytail: no LastAlertedAtUtc cooldown. Add when users report spam.` No migration needed, no `MarkAlerted()`.
- **Reuses `WatchlistMatch` preference**: `// ponytail: reuse WatchlistMatch pref for price drops. Split to PriceDrop pref if users want separate control.`
- **Reuses `SendDealAlertAsync`**: `// ponytail: reuse DealAlertEmailModel for price drops. Add dedicated PriceDropEmailModel when email template diverges.`
- **No `SendPriceDropAsync`** on `IEmailService` — zero interface changes.
- **Alert type string**: uses `"PriceDrop"` (not a new enum value), existing `PendingAlert.Create` accepts any string.

## Tests
- `DealPriceDropNotificationHandlerTests`:
  - Price increase → no repo calls (early return).
  - Category watcher gets alert on price drop.
  - Keyword watcher with `TargetPrice` met gets alert.
