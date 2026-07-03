# DealSpot — Feature Specs

Specs for remaining (unbuilt) features. One file per feature. Ordered by dependency, then roadmap phase.

## Verified current status (code, not docs)

Docs (`DOCUMENTATION.md` §23) drift from code. Actual state:

**Built & wired**
- Domain: `Deal`, `Tenant`, `Category`, `User`, `WatchlistEntry`, `UserNotificationPreference`, `AffiliateNetwork`.
- App: `CreateDeal`, `ReportDealExpired`, `GetDeals` handlers; `Logging` + `Validation` pipeline behaviors.
- Infra: `ApplicationDbContext` (all DbSets), `DealRepository`, `TenantRepository`, `CategoryRepository`, `AffiliateUrlRewriter`, `ResendEmailService`, `DealUrlHealthCheckJob`, `TenantResolutionMiddleware`.
- Web: `Home`, `DealDetail`, `DealCard`, `ExpireReportButton`, `MainLayout`; **Google OAuth is wired** in `Program.cs` (docs wrongly list it as Phase 2 TODO); output cache (3 policies); Hangfire dashboard.
- Tests: `DealTests`, `CreateDealCommandHandlerTests`. Migration: `InitialCreate`.

**Domain-only stubs (entity exists, nothing consumes it)**
- `User`, `WatchlistEntry`, `UserNotificationPreference`, `AffiliateNetwork` — no repositories, handlers, EF configs, or UI.

**Gaps found beyond the roadmap**
- Domain events raised but never dispatched (`SaveChangesAsync` only calls `base`) → see `00`.
- `/categories` linked from `DealDetail` breadcrumb but no page exists → see `08`.
- Cache tags declared but never evicted → see `09`.
- OAuth sign-in creates no domain `User` row → see `01`.
- No EF config for `Category`, `User`, `WatchlistEntry`, `UserNotificationPreference`, `AffiliateNetwork` (convention-only; add as each feature lands).

## Index

| # | Spec | Phase | Depends on |
|---|------|-------|-----------|
| 00 | Domain event dispatch | Foundation | — |
| 01 | User accounts & auth wiring | 2 | — |
| 02 | Watchlist | 2 | 01 |
| 03 | Email alerts & preferences | 2 | 00, 01, 02 |
| 04 | Deal voting | 2 | 01 |
| 05 | Deal Score calculation | 2 | 04 (velocity) |
| 06 | AdSense | 2 | — |
| 07 | Admin deal submission UI | 2 | 01 |
| 08 | Category browse pages | 2 | — |
| 09 | Output cache invalidation | 2 | 00 |
| 10 | Price history | 3 | 00 |
| 11 | Price drop alerts | 3 | 02, 03, 10 |
| 12 | Community submissions & approval | 3 | 01, 07 |
| 13 | User reputation | 3 | 04, 12 |
| 14 | Web push notifications | 3 | 01, 03 |

## Conventions (match existing code)

- Entities: private ctor + `private set`, static `Create(...) : Result<T>`, `RaiseDomainEvent(...)`.
- CQRS: `ICommand`/`ICommand<T>`/`IQuery<T>` (MediatR) returning `Result`/`Result<T>`; one folder per use case under the aggregate.
- Errors: static `XxxErrors` class of `Error(Code, Description)`.
- Repos: interface in Domain, EF impl in Infra; `Add`/`Update` + async gets; persist via `IUnitOfWork.SaveChangesAsync`.
- Validation: `AbstractValidator<TCommand>` (auto-run by `ValidationBehavior`).
- Web: Blazor Static SSR by default; `@rendermode InteractiveServer` only where interaction is needed.
