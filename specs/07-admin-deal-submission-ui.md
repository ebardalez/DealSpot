# 07 — Admin Deal Submission UI

**Phase:** 2 · **Status:** ✅ Done · **Depends on:** 01

## Goal
`CreateDealCommand` + handler exist but there's no way to invoke them. Add an admin-only Blazor form to submit deals (you are the sole submitter at launch).

## Changes

### Web
- `/admin/deals/new` page, `@rendermode InteractiveServer`, `[Authorize(Roles="Admin")]`.
- Form fields → `CreateDealCommand`: category (dropdown of current tenant's categories), title, description, current price, original price (opt), currency (default USD), product URL, image URL (opt), retailer name.
- Submit → `ISender.Send`; on success show generated slug + link to `/deals/{slug}` and the rewritten affiliate URL; on `Result.Failure` show `Error.Description`.
- `/admin/deals` list: recent deals for tenant with expire action (reuse `ReportDealExpiredCommand` or add direct `ExpireDealCommand(reason: Manual)`).
- Category dropdown needs a `GetCategoriesQuery` (see 08) — reuse it.

### Application (small)
- Optional `Deals/Expire/ExpireDealCommand(DealId, reason=Manual)` for admin hard-expire (distinct from community report). Verifies admin via context.

### Infrastructure
- Ensure an `Admin` role exists (seed on startup) and your user is assigned it.

## Acceptance
- Non-admin gets 403/redirect; admin sees the form.
- Valid submit creates a deal, shows slug + affiliate URL.
- Validation failures (bad price/URL) surface inline from `ValidationBehavior`.

## Notes
`// ponytail:` server-rendered form posting a MediatR command — no separate API layer. Add an API only if a non-Blazor client appears.
