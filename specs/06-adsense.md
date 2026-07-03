# 06 — Google AdSense

**Phase:** 2 · **Status:** ✅ Done · **Depends on:** —

## Goal
Display AdSense units (sidebar, below deal description) without hurting the "Get This Deal" click. Per-tenant enable + slot IDs. No interstitials.

## Changes

### Domain / config
- Extend `TenantBranding` (JSONB) with optional `adsenseClientId` (`ca-pub-…`) and slot ids (`adSlotSidebar`, `adSlotInline`). Nullable → tenant with no ids shows no ads. No migration (JSONB).

### Web
- `Components/Shared/AdUnit.razor`: params `Slot`, `Format`; renders the AdSense `<ins class="adsbygoogle">` + `(adsbygoogle=window.adsbygoogle||[]).push({})`. Render nothing when tenant has no client id.
- Load the AdSense script once in `App.razor`/`MainLayout` head, only when `adsenseClientId` present.
- Placements: sidebar on `Home`; inline below `DealDetail` description. Never between the price and the CTA button.
- `ads.txt` endpoint per tenant (`MapGet("/ads.txt", …)`) returning the tenant's publisher line (required by AdSense).

## Acceptance
- Tenant without AdSense config renders zero ad markup and no script.
- CTA button position unchanged by ad slots.
- `/ads.txt` returns the correct publisher id for the resolved tenant.

## Notes
No automated test (third-party script). Manual verify with AdSense preview. Keep `loading` deferred so ads never block SSR/LCP.
