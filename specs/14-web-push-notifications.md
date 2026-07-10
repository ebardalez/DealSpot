# 14 — Web Push Notifications

**Phase:** 3 · **Status:** ✅ Done · **Depends on:** 01, 03

## Goal
Browser push (Web Push / VAPID) as an alternative delivery channel for the same triggers as email (03) + price drops (11). Opt-in; email stays the default.

## Changes

### Domain
- `Users/PushSubscription.cs` (`Entity`): `UserId`, `TenantId`, `Endpoint`, `P256dh`, `Auth`, `CreatedAtUtc`. Factory + `IPushSubscriptionRepository` (`Add`, `Remove`, `GetByUserAsync`, `GetForTenantAsync`).
- Extend `UserNotificationPreference` with `PushEnabled` (default false).

### Application
- `Abstractions/Push/IPushSender { Task SendAsync(PushSubscription sub, PushPayload payload, CancellationToken) }`.
- `Push/Subscribe/SubscribeCommand(endpoint, keys)` / `Unsubscribe/UnsubscribeCommand`.
- In alert handlers (03, 11): if `PushEnabled`, also push. Prune subscriptions on `410 Gone`.

### Infrastructure
- `WebPushSender : IPushSender` using a Web Push library (`WebPush` NuGet) + VAPID keys from config.
- Configs + migration `AddPushSubscriptions`.

### Web
- Service worker (`wwwroot/sw.js`) handling `push` + `notificationclick` → open deal URL.
- Client JS: request permission, `pushManager.subscribe(VAPID_PUBLIC)`, POST keys to `SubscribeCommand`.
- Opt-in toggle on `/settings/notifications`.

## Acceptance
- Opt-in registers a subscription; matching deal delivers a push that opens the deal on click.
- `410`/expired subscription is removed, not retried forever.
- Push off (default) → no browser prompt, email unaffected.

## Notes
`// ponytail:` VAPID + `WebPush` NuGet only — no FCM/APNs SDKs. Web Push covers modern desktop + Android; add native push only if an app ships. Minimal automated test (browser API); unit-test subscription prune on `410`.
