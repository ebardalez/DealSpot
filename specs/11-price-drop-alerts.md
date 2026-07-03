# 11 — Price Drop Alerts

**Phase:** 3 · **Status:** Not built · **Depends on:** 02, 03, 10

## Goal
Email users when a watched keyword's deal drops to/below their `TargetPrice`. `WatchlistEntry.TargetPrice` already exists; consume the drop event from 10 through the alert pipeline from 03.

## Changes

### Application
- `INotificationHandler<DealPriceDroppedDomainEvent>` (`PriceHistory/Notifications/PriceDroppedNotificationHandler`):
  1. Find keyword watches (02) matching the deal title with `TargetPrice != null`.
  2. Keep those where `newPrice <= TargetPrice`.
  3. Respect frequency (instant vs digest, reuse 03) and a per-entry cooldown to avoid repeat spam.
- Add `SendPriceDropAsync(to, deal, oldPrice, newPrice)` to `IEmailService`.

### Domain
- On `WatchlistEntry`: `LastAlertedAtUtc` + `MarkAlerted()` for cooldown (e.g. skip if alerted <24h ago at same-or-higher price).

### Infrastructure
- Migration `AddWatchlistAlertCooldown`.
- Price-drop email template.

## Acceptance
- Drop to ≤ target emails the watcher once; a further tiny drop within cooldown does not re-email.
- Drop above target → no email.
- Digest users get it batched, not instant.

## Test
`PriceDroppedNotificationHandler`: target met → send + `MarkAlerted`; within cooldown → skip; above target → skip.
