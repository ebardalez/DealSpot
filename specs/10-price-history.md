# 10 — Price History

**Phase:** 3 · **Status:** Not built · **Depends on:** 00

## Goal
Track price over time per deal/product to power price-drop alerts (11) and the strongest Deal Score signal (05). Amazon via Keepa API; niche retailers via a lightweight HTTP scraper. Store the clean `OriginalUrl` (already captured) as the price key.

## Changes

### Domain
- `PriceHistory/PricePoint.cs` (`Entity`): `DealId` (or product key), `Price`, `Currency`, `Source` (`Keepa`/`Scraper`), `ObservedAtUtc`.
- `IPriceHistoryRepository`: `Add`, `GetForDealAsync(dealId, sinceUtc)`, `GetLatestAsync(dealId)`.

### Application
- `Abstractions/Pricing/IPriceProvider`: `Task<PricePoint?> GetCurrentPriceAsync(string canonicalUrl, CancellationToken)`.
- On new point below current deal price → raise `DealPriceDroppedDomainEvent(dealId, oldPrice, newPrice)` (consumed by 11).

### Infrastructure
- `KeepaPriceProvider` (Keepa API, Amazon domains; key via config/env).
- `ScraperPriceProvider` (`HttpClient` + CSS/JSON-LD `price` selector per retailer domain; config-driven selectors).
- `PriceProviderRouter` picks by domain (Amazon → Keepa else scraper).
- Hangfire `PriceRefreshJob` (`Cron` ~6–12h): batch active deals, fetch, append `PricePoint`, update deal price if changed.
- Configs + migration `AddPriceHistory` (consider Timescale/hypertable later; plain table for MVP).

### Web
- Optional sparkline on `DealDetail` from `GetForDealAsync` (defer if not needed).

## Acceptance
- Amazon deal routes to Keepa; non-Amazon to scraper.
- Refresh appends a point; a drop below stored price emits `DealPriceDroppedDomainEvent`.
- Scraper failure logs and skips (no crash, no bad point).

## Test
`PriceProviderRouter`: domain routing. Handler: drop emits event; equal/higher does not.

> `// ponytail:` config-driven CSS selectors, no headless browser. Add a browser (Playwright) only for JS-rendered retailers that a plain fetch can't read.
