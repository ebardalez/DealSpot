# 02 — Watchlist

**Phase:** 2 · **Status:** ✅ Done · **Depends on:** 01

## Goal
Let signed-in users watch a category or a keyword (with optional target price). Backs email alerts (03) and price-drop alerts (11). `WatchlistEntry` entity + factories already exist; add repository, use cases, and UI.

## Changes

### Domain
- `IWatchlistRepository`: `Add`, `Remove(WatchlistEntry)`, `GetByUserAsync(userId, tenantId)`, `ExistsAsync(userId, tenantId, categoryId?, keyword?)`, `GetCategoryWatchersAsync(tenantId, categoryId)`, `GetKeywordWatchersAsync(tenantId)` (last two consumed by 03).
- `WatchlistErrors`: `Duplicate`, `NotFound`, `NotOwner`.
- (Reuse `CreateCategoryWatch` / `CreateKeywordWatch` / `Deactivate`.)

### Application
- `Watchlist/AddCategoryWatch/AddCategoryWatchCommand(CategoryId)` — tenant + user from context; reject duplicate.
- `Watchlist/AddKeywordWatch/AddKeywordWatchCommand(Keyword, TargetPrice?)`.
- `Watchlist/RemoveWatch/RemoveWatchCommand(WatchlistEntryId)` — verify ownership → `NotOwner`.
- `Watchlist/GetWatchlist/GetWatchlistQuery` → list of `WatchlistItemDto`.
- Validators: keyword non-empty/≤100 chars; `TargetPrice > 0` when provided.

### Infrastructure
- `WatchlistRepository`; `WatchlistConfiguration` (index `(user_id, tenant_id)`).
- Migration `AddWatchlist`.

### Web
- `/watchlist` page (`InteractiveServer`), `[Authorize]`: list entries, remove button, add-keyword form.
- "Watch this category" control on category pages (08); "Notify me" on deal/keyword.

## Acceptance
- Duplicate category watch for same user/tenant → `WatchlistErrors.Duplicate`.
- Remove by non-owner → `NotOwner`, no delete.
- `GetWatchlist` returns only the current user's active entries for the current tenant.

## Test
`AddCategoryWatchCommandHandler`: duplicate rejected; `RemoveWatchCommandHandler`: non-owner blocked.
