# 05 — Deal Score Calculation

**Phase:** 2 · **Status:** ✅ Done · **Depends on:** 04 (velocity)

## Goal
Populate `DealScore` (0–100) so the badge shows. `DealScore` record, component fields, `Label`, and `Deal.UpdateDealScore(...)` already exist and the UI already hides the badge when value is 0. Compute the score.

## Formula (MVP)
`Value = round(0.4·Discount + 0.3·RetailerTrust + 0.3·Velocity)`, each component 0–100.
- **Discount** = `min(DiscountPercentage, 100)` (0 when no `OriginalPrice`).
- **RetailerTrust** = lookup by `RetailerName` (Amazon/Best Buy/Walmart/Newegg = 90; known list = 70; else 40). Static map for MVP.
- **Velocity** = net votes in first 30 min after creation, scaled (e.g. `min(net·10, 100)`); 0 until votes exist.

## Changes

### Application
- `Abstractions/Scoring/IRetailerTrustProvider` → `int TrustFor(string retailerName)`.
- `Deals/Scoring/DealScoreCalculator` (pure): `(deal, netVotesFirst30) => DealScore` with component breakdown + `CalculatedAtUtc`.
- Query on vote repo: net votes within 30 min of `CreatedAtUtc`.

### Infrastructure
- `RetailerTrustProvider` (static dictionary; `// ponytail:` hard-coded map, move to `retailer_trust` table only when it needs editing without deploy).
- Hangfire `DealScoreCalculatorJob` (`Cron.Hourly`): recompute active deals updated in last hour or unscored → `deal.UpdateDealScore(...)`, persist.
- Also recompute inline on `DealCreatedDomainEvent` for an immediate non-velocity score (discount+trust).

### Web
- No new UI — badge already renders when `DealScore > 0`. Add `DealScore` badge to `DealCard` if absent.

## Acceptance
- Deep discount from Amazon scores higher than shallow discount from unknown seller.
- No `OriginalPrice` → discount component 0, still scored on trust.
- Badge appears once score > 0; label matches thresholds.

## Test
`DealScoreCalculator`: given fixed inputs, asserts exact `Value` and `Label`.
