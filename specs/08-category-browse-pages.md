# 08 — Category Browse Pages

**Phase:** 2 · **Status:** Not built (linked, 404s today) · **Depends on:** —

## Goal
`DealDetail` breadcrumb links to `/categories` and `/categories/{slug}` but no page exists → dead links, and `GetDealsQuery` already supports category filtering that nothing calls. Add category listing + category deal feed. Fixes the empty `CategoryName` in `DealSummaryDto`.

## Changes

### Domain / repo
- `ICategoryRepository` — add `GetBySlugAsync(tenantId, slug)`, `GetActiveByTenantAsync(tenantId)` (has `GetByIdAsync` today).

### Application
- `Categories/GetCategories/GetCategoriesQuery` → active categories for tenant (tree: root + one nesting level) as `CategoryDto(Id, Name, Slug, ParentId)`.
- Fix `GetDealsQueryHandler`: populate `CategoryName` (join category name; currently `string.Empty`). Simplest: repo returns deals with category name, or handler loads categories once and maps.

### Web
- `/categories` — grid/list of categories (`CategoryPage` cache policy).
- `/categories/{categorySlug}` — resolve slug → category, call `GetDealsQuery(tenantId, categoryId, page, size)`, reuse `DealCard` grid + paging. `CategoryPage` policy already varies by `categorySlug`.
- Category nav in `MainLayout` header.
- Fix `DealDetail` breadcrumb to link `/categories/{slug}` not bare `/categories`.

## Acceptance
- `/categories/{slug}` shows only that category's active deals for the tenant; unknown slug → 404.
- Breadcrumb category name is populated (no blank).
- Categories from another tenant never appear.

## Test
`GetCategoriesQueryHandler`: returns only active, current-tenant categories, ordered by `SortOrder`.
