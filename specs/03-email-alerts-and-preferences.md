# 03 — Email Alerts & Notification Preferences

**Phase:** 2 · **Status:** Not built (email transport exists) · **Depends on:** 00, 01, 02

## Goal
Email users when a new deal matches their watchlist. Respect per-tenant preferences and instant-vs-digest frequency. `ResendEmailService` (`IEmailService`) already sends; wire the triggers. `UserNotificationPreference` entity exists.

## Triggers (MVP)
| Trigger | Fires when | Preference flag |
|---|---|---|
| New deal in category | deal created in a watched category | `NewDealInCategory` |
| Watchlist keyword match | new deal title contains a watched keyword | `WatchlistMatch` |
| Deal expiring soon | deal near expiry | `DealExpiringSoon` (default off) |

## Changes

### Domain
- `INotificationPreferenceRepository`: `GetAsync(userId, tenantId)`, `Add`.
- (Reuse `UserNotificationPreference.Create/Update`, `NotificationFrequency`.)

### Application
- `INotificationHandler<DealCreatedDomainEvent>` (`Deals/Notifications/DealCreatedNotificationHandler`):
  1. Load deal + category.
  2. Category watchers (02) with `NewDealInCategory` on.
  3. Keyword watchers whose keyword ∈ title, `WatchlistMatch` on.
  4. Instant pref → `IEmailService` now; Digest pref → enqueue `PendingAlert`.
- `NotificationPreferences/Update/UpdatePreferencesCommand`; `Get/GetPreferencesQuery`.
- `Abstractions/Email` — add `SendDealAlertAsync(to, deal)` + `SendDigestAsync(to, deals)` if missing.

### Infrastructure
- `NotificationPreferenceRepository`; configs + migration `AddNotificationPreferences` (+ `pending_alerts` table for digest).
- Hangfire `DailyDigestJob` (`Cron.Daily`): group `PendingAlert` by user, one email, clear sent.
- Email templates (deal alert, digest) — minimal HTML.

### Web
- `/settings/notifications` (`InteractiveServer`, `[Authorize]`): toggles + frequency; default-create prefs on first view.

## Acceptance
- New deal → instant users emailed once; digest users get zero immediate mail but appear in next digest.
- Preference off → no email for that trigger.
- Digest batches multiple matches into one email and clears the queue.

## Test
`DealCreatedNotificationHandler`: category+keyword matchers resolved, instant emails sent, digest users enqueued not emailed.

> `// ponytail:` in-DB `pending_alerts` table for digest, no message broker. Add a broker only if volume outgrows a daily batch query.
