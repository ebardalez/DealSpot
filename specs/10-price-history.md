# 10 — Price History

**Phase:** 3 · **Status:** ✅ Done · **Depends on:** 00

## Goal
Track price over time per deal to power price-drop alerts (11) and inform buyers. Record snapshots on deal creation and admin price updates.

## What Was Built

### Domain
- `PriceSnapshot` entity: `DealId`, `Price`, `OriginalPrice`, `Currency`, `Source` ("initial"/"admin_update"/"scraper"), `RecordedAtUtc`. Static factory `Record(...)`.
- `IPriceSnapshotRepository`: `GetByDealAsync(dealId)`, `Add(snapshot)`.
- `DealPriceChangedDomainEvent`: carries `DealId`, `TenantId`, `OldPrice`, `NewPrice`, `NewOriginalPrice`, `Currency`.
- `Deal.UpdatePrice(newCurrent, newOriginal)`: validates, updates `DealPrice` VO, raises `DealPriceChangedDomainEvent`. No-op if unchanged.

### Application
- `RecordPriceSnapshotHandler`: implements `INotificationHandler<DealCreatedDomainEvent>` + `INotificationHandler<DealPriceChangedDomainEvent>`. Records a snapshot on each event.
- `GetPriceHistoryQuery` → `IReadOnlyList<PriceSnapshotDto>` ordered chronologically.
- `UpdateDealPriceCommand` → loads deal, calls `UpdatePrice`, persists. Domain event pipeline handles the rest.

### Infrastructure
- `PriceSnapshotConfiguration`: `price_snapshots` table, index on `(deal_id, recorded_at_utc)`.
- `PriceSnapshotRepository`: EF Core impl.
- Registered `IPriceSnapshotRepository` in DI. Added `PriceSnapshots` DbSet to `ApplicationDbContext`.

### Web
- `DealDetail.razor`: CSS bar chart of price history (only renders when >1 snapshot). Shows low/high range.
- `AdminDealPrice.razor`: `/admin/deals/{id}/price` — update price form + history table. "Price" button on admin deal list.

## Deviations from Original Spec
- **No Keepa API / scraper**: `// ponytail: record prices on admin updates only. Add scraper job when price-tracking API/service is available.` No `IPriceProvider`, no `PriceProviderRouter`, no `PriceRefreshJob`.
- **Simpler entity name**: `PriceSnapshot` instead of `PricePoint`.
- **No sparkline**: inline CSS bar chart instead.
- **Event name**: `DealPriceChangedDomainEvent` instead of `DealPriceDroppedDomainEvent` — covers all price changes, not just drops. Drop detection moved to the alert handler (spec 11).

## Tests
- `UpdateDealPriceCommandHandlerTests`: price update persists, same-price no-op, not found, invalid price.
- `RecordPriceSnapshotHandlerTests`: initial snapshot on create, snapshot on price change.

## Migration
```bash
dotnet ef migrations add AddPriceSnapshots --project src/DealSpot.Infrastructure --startup-project src/DealSpot.Web --output-dir Persistence/Migrations
```
Auto-applied on startup.
