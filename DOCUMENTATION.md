# DealSpot — Project Documentation

> Multi-tenant niche deal aggregator platform. One codebase, multiple niche sites (tech, aquarium, supplements), each running on its own subdomain with independent branding, categories, and affiliate configurations. Built with .NET 10 Blazor SSR, PostgreSQL, and Docker.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture](#2-architecture)
3. [Domain Model](#3-domain-model)
4. [Application Layer — CQRS](#4-application-layer--cqrs)
5. [Infrastructure Layer](#5-infrastructure-layer)
6. [Web Layer — Blazor SSR](#6-web-layer--blazor-ssr)
7. [Multi-Tenancy](#7-multi-tenancy)
8. [Affiliate URL Rewriting](#8-affiliate-url-rewriting)
9. [SEO Strategy](#9-seo-strategy)
10. [Authentication](#10-authentication)
11. [Caching Strategy](#11-caching-strategy)
12. [Background Jobs](#12-background-jobs)
13. [Email Notifications](#13-email-notifications)
14. [Deal Score](#14-deal-score)
15. [Database](#15-database)
16. [Environment Configuration](#16-environment-configuration)
17. [Local Development Setup](#17-local-development-setup)
18. [Production Deployment](#18-production-deployment)
19. [Adding a New Tenant](#19-adding-a-new-tenant)
20. [Adding Affiliate Networks](#20-adding-affiliate-networks)
21. [Testing](#21-testing)
22. [Monetization Strategy](#22-monetization-strategy)
23. [Development Roadmap](#23-development-roadmap)
24. [Key Architectural Decisions](#24-key-architectural-decisions)

---

## 1. Project Overview

DealSpot is a deal aggregator platform inspired by PZDeals and Slickdeals. The core concept is to run multiple niche deal sites from a single codebase and deployment, differentiated by subdomain:

| Niche | Domain | Example |
|---|---|---|
| Technology | `tech.yourdomain.com` | GPUs, laptops, phones |
| Aquarium | `aquarium.yourdomain.com` | Filters, lights, live plants |
| Supplements | `supplements.yourdomain.com` | Protein, vitamins, pre-workout |

**Primary revenue model:** affiliate commissions. Every deal URL submitted — whether by you or the community — is automatically rewritten to include your affiliate tag before it is stored. Clicking "Get This Deal" sends the user to the retailer through your tracked link.

**Secondary revenue model:** Google AdSense display ads (Phase 2).

**Go-to-market:** You are the sole deal submitter at launch. SEO and Google organic search is the only acquisition channel at MVP. Each deal gets a dedicated, indexable URL with full structured data markup.

---

## 2. Architecture

### Solution Structure

```
DealSpot/
├── src/
│   ├── DealSpot.Domain/          # Core business logic — zero external dependencies
│   ├── DealSpot.Application/     # Use cases — CQRS commands, queries, MediatR handlers
│   ├── DealSpot.Infrastructure/  # External concerns — EF Core, Hangfire, email, affiliate
│   └── DealSpot.Web/             # Blazor SSR presentation layer
├── tests/
│   ├── DealSpot.Domain.Tests/    # Pure domain unit tests
│   └── DealSpot.Application.Tests/ # Command/query handler tests with NSubstitute
└── docker/
    ├── Dockerfile                # Multi-stage production build
    ├── docker-compose.yml        # Full stack: app + PostgreSQL + Redis
    └── .env.example              # Environment variable template
```

### Layer Dependency Rule

Dependencies flow strictly inward. No layer may reference a layer outside its boundary:

```
DealSpot.Web
    └── depends on → DealSpot.Application
                         └── depends on → DealSpot.Domain
DealSpot.Infrastructure
    └── depends on → DealSpot.Domain
    └── depends on → DealSpot.Application   (implements its interfaces)
```

The Domain layer has **zero** dependencies on any other layer or external NuGet package (except `MediatR.Contracts` for `IDomainEvent`).

### Tech Stack

| Concern | Technology | Rationale |
|---|---|---|
| Framework | .NET 10 Blazor SSR | Single language full-stack; SSR is required for SEO |
| Database | PostgreSQL 17 | Free, JSONB for tenant branding config, full-text search built-in |
| ORM | EF Core 10 + Dapper | EF for writes; Dapper for optimised read queries |
| Background Jobs | Hangfire | Persistent job queue backed by PostgreSQL |
| Email | Resend | 3,000 free emails/month; clean .NET SDK |
| Auth | ASP.NET Core Identity + Google OAuth | Social login removes password friction |
| Caching | .NET 10 Output Cache | Native; no separate cache server required at MVP |
| CDN / Proxy | Cloudflare (free tier) | Edge caching, DDoS protection, SSL, wildcard subdomain routing |
| Hosting | Hetzner VPS + Docker Compose | ~€4/month CX22; cheapest path to production |
| Logging | Serilog | Structured logging to console + rolling file |
| Testing | xUnit + NSubstitute + FluentAssertions | Standard .NET testing trio |

---

## 3. Domain Model

All domain entities live in `DealSpot.Domain`. The domain has no knowledge of databases, HTTP, or infrastructure concerns.

### Entity Base Class

Every aggregate root and entity inherits from `Entity`:

```csharp
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public Guid Id { get; init; }
    public IReadOnlyList<IDomainEvent> GetDomainEvents();
    public void ClearDomainEvents();
    protected void RaiseDomainEvent(IDomainEvent domainEvent);
}
```

### Result Pattern

All domain factory methods and business operations return `Result<T>` instead of throwing exceptions for expected failures:

```csharp
// Success
Result.Success(deal)
Result.Success<CreateDealResponse>(response)

// Failure
Result.Failure<Deal>(DealErrors.InvalidTitle)
Result.Failure(DealErrors.AlreadyExpired)
```

Callers always check `result.IsSuccess` / `result.IsFailure` before accessing `result.Value`.

### Aggregate: Deal

The core aggregate of the system. Located at `DealSpot.Domain/Deals/Deal.cs`.

**Key properties:**

| Property | Type | Description |
|---|---|---|
| `TenantId` | `Guid` | Which niche site this deal belongs to |
| `CategoryId` | `Guid` | Tenant-scoped category |
| `Title` | `string` | Deal headline (max 200 chars) |
| `Slug` | `string` | SEO URL slug — unique per tenant, e.g. `sony-wh1000xm5-279` |
| `Price` | `DealPrice` | Owned value object: current price, original price, currency |
| `OriginalUrl` | `string` | Clean canonical URL — tracking params stripped, used for price history |
| `AffiliateUrl` | `string` | Monetized outbound URL with your affiliate tag injected |
| `Status` | `DealStatus` | `Active`, `Expired`, `PendingApproval`, `Rejected` |
| `ExpiredReportCount` | `int` | Community reports; auto-expires at 5 |
| `DealScore` | `DealScore` | Composite quality score 0–100 |

**Business rules enforced by the domain:**
- Title cannot be empty — `DealErrors.InvalidTitle`
- Affiliate URL cannot be empty — `DealErrors.InvalidUrl`
- Current price must be > 0 — `DealErrors.InvalidPrice`
- `ReportAsExpired()` auto-expires the deal when `ExpiredReportCount >= 5`
- `ReportAsExpired()` on an already-expired deal returns `DealErrors.AlreadyExpired`
- `DealCreatedDomainEvent` is raised on creation
- `DealExpiredDomainEvent` is raised on expiration (with reason: `Manual`, `CommunityReported`, `UrlDead`, `Scheduled`)
- `DealPriceChangedDomainEvent` is raised when price is updated via `UpdatePrice()` (carries old/new price)

### Value Object: DealPrice

```csharp
public sealed record DealPrice(decimal CurrentPrice, decimal? OriginalPrice, string Currency)
{
    // Derived — not stored separately
    public decimal? DiscountPercentage => OriginalPrice.HasValue && OriginalPrice > 0
        ? Math.Round((1 - CurrentPrice / OriginalPrice.Value) * 100, 1)
        : null;
}
```

Stored as an EF Core owned type with columns `current_price`, `original_price`, `currency`.

### Value Object: DealScore

```csharp
public sealed record DealScore(int Value, decimal DiscountComponent,
    decimal RetailerTrustComponent, decimal VelocityComponent, DateTime CalculatedAtUtc)
{
    public static readonly DealScore NotCalculated = new(0, 0, 0, 0, DateTime.MinValue);

    public string Label => Value switch
    {
        >= 85 => "🔥 Excellent",
        >= 70 => "👍 Great",
        >= 50 => "✅ Good",
        >= 30 => "😐 Fair",
        _     => "⚠️ Weak"
    };
}
```

Score components (Phase 2 implementation):
- **DiscountComponent** — how deep the discount is vs MSRP
- **RetailerTrustComponent** — known retailer reputation (Amazon > unknown dropshipper)
- **VelocityComponent** — how fast the deal is accumulating community votes

### Aggregate: Tenant

Represents one niche site. Each tenant has its own branding, categories, and affiliate configurations stored in the same database with `TenantId` as the isolation key.

**Key properties:**

| Property | Description |
|---|---|
| `Slug` | Short identifier: `tech`, `aquarium`, `supplements` |
| `Domain` | Full subdomain: `tech.yourdomain.com` |
| `Branding` | `TenantBranding` — stored as JSONB in PostgreSQL |

**TenantBranding (JSONB):**
```json
{
  "primaryColor": "#e63946",
  "secondaryColor": "#1d3557",
  "logoUrl": "/images/tech-logo.svg",
  "faviconUrl": "/favicon.ico",
  "metaDescription": "Best tech deals today.",
  "metaKeywords": "tech deals, laptop deals, GPU deals"
}
```

### Entity: Category

Tenant-scoped category tree. Each tenant has a completely independent category taxonomy — tech categories look nothing like aquarium categories.

- `TenantId` — scopes the category to its tenant
- `ParentId` — supports one level of nesting (root + subcategory)
- `Slug` — used in URLs: `/categories/headphones`

### Entity: User

Shared across tenants — one account works on all your niche sites.

- `Email`, `DisplayName`, `AvatarUrl`
- `ReputationScore` — future reputation system seed
- Uses ASP.NET Core Identity for authentication; this entity holds deal-site-specific user data

### Entity: WatchlistEntry

Supports two watch modes:

| Mode | Factory | Behaviour |
|---|---|---|
| Category watch | `WatchlistEntry.CreateCategoryWatch(userId, tenantId, categoryId)` | Email when any new deal is posted in this category |
| Keyword watch | `WatchlistEntry.CreateKeywordWatch(userId, tenantId, keyword, targetPrice?)` | Email when a deal matching keyword is posted; `TargetPrice` enables price-drop alerts (Phase 3) |

### Entity: UserNotificationPreference

Per-user, per-tenant settings controlling which of the three email alert types are active and whether delivery is instant or daily digest.

### Entity: AffiliateNetwork

Maps a retailer domain to your affiliate tag for a specific tenant. The URL rewriting pipeline consults this table on every deal submission.

```
RetailerDomain: "amazon.com"
NetworkName:    "Amazon Associates"
AffiliateTag:   "techdeals-20"
UrlTemplate:    "{url}?tag={tag}"
```

### Entity: PriceSnapshot

Records a deal's price at a point in time. Created automatically via domain event handlers when a deal is created (`source: "initial"`) or when an admin updates a deal's price (`source: "admin_update"`). Future scraper jobs will add `source: "scraper"` snapshots.

| Property | Type | Description |
|---|---|---|
| `DealId` | `Guid` | Which deal this snapshot belongs to |
| `Price` | `decimal` | Current price at time of recording |
| `OriginalPrice` | `decimal?` | Original/MSRP price at time of recording |
| `Currency` | `string` | 3-letter ISO currency code |
| `Source` | `string` | "initial", "admin_update", or "scraper" |
| `RecordedAtUtc` | `DateTime` | When the snapshot was taken |

---

## 4. Application Layer — CQRS

All use cases live in `DealSpot.Application` as MediatR commands and queries. The layer depends only on the Domain — no EF Core, no HTTP.

### Commands

| Command | Handler | What it does |
|---|---|---|
| `CreateDealCommand` | `CreateDealCommandHandler` | Validates category ownership, rewrites affiliate URL, generates slug, persists deal |
| `ReportDealExpiredCommand` | `ReportDealExpiredCommandHandler` | Increments expired report count; auto-expires at 5 reports |
| `UpdateDealPriceCommand` | `UpdateDealPriceCommandHandler` | Updates deal price, raises `DealPriceChangedDomainEvent` |

### Queries

| Query | Handler | What it returns |
|---|---|---|
| `GetDealsQuery` | `GetDealsQueryHandler` | Paginated list of active deals for a tenant, optionally filtered by category |
| `GetPriceHistoryQuery` | `GetPriceHistoryQueryHandler` | Chronological list of price snapshots for a deal |

### MediatR Pipeline Behaviors

All commands and queries pass through two pipeline behaviors registered in order:

**1. LoggingBehavior** — logs request start, logs success or failure with error code on completion.

**2. ValidationBehavior** — runs all `IValidator<TCommand>` implementations via FluentValidation before the handler is called. Returns `Result.Failure` with a consolidated error message if any rules fail — the handler never executes.

### CreateDealCommand — Full Flow

```
HTTP POST /admin/deals
    → Blazor component calls ISender.Send(CreateDealCommand)
    → LoggingBehavior logs request
    → ValidationBehavior runs CreateDealCommandValidator
        - TenantId not empty
        - CategoryId not empty
        - Title not empty, max 200 chars
        - CurrentPrice > 0
        - OriginalPrice > CurrentPrice (if provided)
        - Currency is 3-letter ISO code
        - ProductUrl is valid absolute URL
    → CreateDealCommandHandler.Handle()
        1. Loads category, validates it belongs to this tenant
        2. Calls IAffiliateUrlRewriter.RewriteAsync() — strips tracking params, injects affiliate tag
        3. Generates SEO slug: "{title-kebab-case}-{price}"
        4. Calls Deal.Create() — domain validates, raises DealCreatedDomainEvent
        5. IDealRepository.Add(deal)
        6. IUnitOfWork.SaveChangesAsync()
    → Returns CreateDealResponse { DealId, Slug, AffiliateUrl }
```

### Abstractions Defined in Application

The Application layer defines interfaces that Infrastructure must implement. This keeps the Application layer free of infrastructure concerns:

| Interface | Purpose |
|---|---|
| `IAffiliateUrlRewriter` | Strips tracking params and injects affiliate tag |
| `IEmailService` | Sends deal alert and watchlist match emails |
| `ITenantContext` | Provides the current resolved tenant for the request |
| `IDateTimeProvider` | Abstraction over `DateTime.UtcNow` for testability |

---

## 5. Infrastructure Layer

Implements all interfaces defined in the Application layer. Located in `DealSpot.Infrastructure`.

### Persistence

**ApplicationDbContext** extends `IdentityDbContext` (for ASP.NET Core Identity) and implements `IUnitOfWork`.

DbSets:
- `Tenants`, `Categories`, `Deals`, `AffiliateNetworks`
- `AppUsers`, `WatchlistEntries`, `NotificationPreferences`

All tables use snake_case naming via `UseSnakeCaseNamingConvention()`.

**EF Core Configurations** (via `IEntityTypeConfiguration<T>`):

`DealConfiguration` maps `DealPrice` and `DealScore` as owned types with explicit column names. Key indexes:
- `(TenantId, Status)` — powers all active deal feeds
- `(TenantId, CategoryId, Status)` — category-filtered feeds
- `(TenantId, Slug)` UNIQUE — guarantees slug uniqueness per tenant
- `CreatedAtUtc` — ordering by newest

`TenantConfiguration` stores `TenantBranding` as a JSONB column using EF Core 8+ `ToJson()`.

### Repositories

| Repository | Interface | Notes |
|---|---|---|
| `DealRepository` | `IDealRepository` | LINQ queries with EF Core |
| `TenantRepository` | `ITenantRepository` | Lookup by id, slug, or domain |

### AffiliateUrlRewriter

1. Parses the raw URL
2. Extracts the domain (strips `www.`)
3. Builds canonical URL by removing all known tracking parameters
4. Looks up `AffiliateNetwork` record matching `(TenantId, RetailerDomain)`
5. If found, calls `network.BuildAffiliateUrl(canonicalUrl)` using the URL template
6. Returns both the affiliate URL (for outbound clicks) and the canonical URL (for price history storage)

Tracking params stripped: `ref`, `tag`, `linkCode`, `linkId`, `camp`, `creative`, `utm_*`, `cid`, `sid`, `affid`, `aff_id`, `affiliate`.

### TenantResolutionMiddleware

Runs on every request before authentication. Reads `context.Request.Host.Host` (the subdomain), looks up the matching `Tenant` in the database, and stores it in the scoped `TenantContext`. Tenant lookups are cached in-memory for 30 minutes to avoid a DB hit on every request.

If no tenant is found for the host, returns HTTP 404.

### DealUrlHealthCheckJob

Hangfire recurring job running every 6 hours. Processes active deals in batches of 100. For each deal it sends an HTTP HEAD request to `OriginalUrl`:
- `404` or `5xx` → marks deal as expired with reason `UrlDead`
- `HttpRequestException` (DNS failure, connection refused) → marks as expired
- `TaskCanceledException` (timeout) → skips, checks next run

Retries up to 2 times via `[AutomaticRetry(Attempts = 2)]`.

### ResendEmailService

Implements `IEmailService` using the Resend SDK. Sends two email types:
1. **Deal alert** — triggered when a new deal matches a user's category or keyword watchlist
2. **Watchlist match** — groups multiple matching deals into a single email for keyword watches

### DependencyInjection

`AddInfrastructure(services, configuration)` registers everything. `UseInfrastructure(app)` schedules the Hangfire recurring job.

---

## 6. Web Layer — Blazor SSR

Located in `DealSpot.Web`. Uses .NET 10 Blazor with Static Server-Side Rendering (SSR) as the default render mode. Interactive components (like the expire report button) use `@rendermode InteractiveServer` selectively.

### Pages

| Route | File | Render Mode | Cache Policy |
|---|---|---|---|
| `/` | `Home.razor` | Static SSR + StreamRendering | `CategoryPage` (10 min) |
| `/deals/{slug}` | `DealDetail.razor` | Static SSR | `DealPage` (30 min, varies by slug) |

### Admin Pages

| Route | File | Description |
|---|---|---|
| `/admin/deals` | `AdminDealList.razor` | List all deals with expire + price update actions |
| `/admin/deals/new` | `AdminDealNew.razor` | Create a new deal |
| `/admin/deals/{id}/price` | `AdminDealPrice.razor` | Update deal price + view price history |

### Shared Components

| Component | Description |
|---|---|
| `DealCard.razor` | Deal summary card for the feed grid — pure static SSR |
| `ExpireReportButton.razor` | Interactive "Deal Expired?" button — uses `@rendermode InteractiveServer` |

### MainLayout

Reads `TenantContext.Current.Branding` and injects CSS custom properties into the root element:

```html
<div style="--color-primary: #e63946; --color-secondary: #1d3557">
```

This makes the entire UI skin configurable per tenant from a single database value with no code changes.

Also handles the auth nav — shows "My Watchlist" + sign-out for authenticated users, "Sign in with Google" for guests.

### Middleware Pipeline Order

```
UseHttpsRedirection
UseStaticFiles
UseSerilogRequestLogging
UseMiddleware<TenantResolutionMiddleware>   ← must be before auth
UseAuthentication
UseAuthorization
UseOutputCache
UseAntiforgery
UseHangfireDashboard("/admin/jobs")
MapRazorComponents
MapHealthChecks("/health")
```

---

## 7. Multi-Tenancy

### Strategy

Single deployment, single database, `TenantId` column on every core table. All queries are scoped to the current tenant automatically via `ITenantContext`.

### How Tenant Resolution Works

1. Request arrives at Hetzner server
2. Cloudflare proxies it (wildcard DNS: `*.yourdomain.com → Hetzner IP`)
3. `TenantResolutionMiddleware` reads `Host` header (e.g. `tech.yourdomain.com`)
4. Looks up tenant by domain — cached in-memory for 30 minutes
5. Sets `TenantContext.Current` for the lifetime of the request
6. All Blazor pages and handlers inject `ITenantContext` to get `TenantContext.Current.Id`

### Adding a New Niche

1. Insert a row into `tenants` table (see [Section 19](#19-adding-a-new-tenant))
2. Add a Cloudflare DNS record pointing the new subdomain to your server IP
3. Insert affiliate network mappings for that tenant (see [Section 20](#20-adding-affiliate-networks))
4. No code changes, no redeployment required

### Data Isolation

Each of the following tables has a `TenantId` column and all queries filter by it:
- `deals` — indexed on `(tenant_id, status)` and `(tenant_id, category_id, status)`
- `categories` — entirely scoped; tech and aquarium categories never mix
- `affiliate_networks` — each tenant has its own retailer → affiliate tag mappings
- `watchlist_entries` — users watch deals on a specific niche site
- `user_notification_preferences` — notification settings per user per tenant

---

## 8. Affiliate URL Rewriting

Every deal URL submitted to the system goes through automatic affiliate rewriting before it is stored. This happens inside `CreateDealCommandHandler` and is transparent to the submitter.

### The Two URLs

| Field | Content | Purpose |
|---|---|---|
| `OriginalUrl` | Canonical URL, all tracking params stripped | Price history tracking (Phase 3) |
| `AffiliateUrl` | Your affiliate tag injected via template | Outbound click monetization |

### URL Template System

Each `AffiliateNetwork` row defines a URL template with two placeholders:

```
Template: "{url}?tag={tag}"
Tag:      "techdeals-20"
Input:    "https://amazon.com/dp/B0BDHWDR12?ref=sr_1_1"

Step 1 — strip tracking params → "https://amazon.com/dp/B0BDHWDR12"
Step 2 — apply template       → "https://amazon.com/dp/B0BDHWDR12?tag=techdeals-20"
```

### Supported Networks (examples)

```sql
-- Amazon Associates
('{url}?tag={tag}')  → https://amazon.com/dp/B123?tag=yoursite-20

-- CJ / Impact (varies by advertiser)
('{url}&affid={tag}')  → https://bestbuy.com/product/123?affid=yoursite
```

### Passthrough Behaviour

If no `AffiliateNetwork` row exists for the deal's domain + tenant, the canonical URL is stored as-is in both `OriginalUrl` and `AffiliateUrl`. The deal is still published — you simply don't earn commission on that click. Add the network row later and update the deal's `AffiliateUrl`.

---

## 9. SEO Strategy

SEO is the sole acquisition channel at launch. Every architectural decision in the web layer is made to maximise Google discoverability.

### Dedicated Deal URLs

Each deal has a unique, human-readable URL:
```
/deals/sony-wh1000xm5-noise-cancelling-headphones-279
/deals/amazon-echo-dot-5th-gen-18
```

Slugs are generated from `"{title-kebab-case}-{price}"` and guaranteed unique per tenant via a database unique index.

### JSON-LD Structured Data

Every deal page emits two JSON-LD blocks in `<HeadContent>`:

**Product + Offer schema** — enables Google Shopping / rich results:
```json
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "Sony WH-1000XM5",
  "offers": {
    "@type": "Offer",
    "price": "279.99",
    "priceCurrency": "USD",
    "availability": "https://schema.org/InStock",
    "seller": { "@type": "Organization", "name": "Amazon" }
  }
}
```

**BreadcrumbList schema** — improves SERP appearance with breadcrumb trails:
```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Home" },
    { "@type": "ListItem", "position": 2, "name": "Headphones" },
    { "@type": "ListItem", "position": 3, "name": "Sony WH-1000XM5" }
  ]
}
```

The home page emits an **ItemList schema** covering all active deals.

### Open Graph Tags

All deal pages include full OG tags for rich previews when shared on WhatsApp, Reddit, or social media:
```html
<meta property="og:title" content="Sony WH-1000XM5 — $279.99 — TechDeals" />
<meta property="og:image" content="https://..." />
<meta property="og:url" content="https://tech.yourdomain.com/deals/sony-wh1000xm5-279" />
```

### Canonical URLs

Every page includes a `<link rel="canonical">` pointing to its definitive URL, preventing duplicate content issues across tenants.

### Performance

- Blazor SSR renders complete HTML on the server — Google bots receive fully rendered content with zero JavaScript execution required
- Output cache + Cloudflare edge cache means most bot crawls hit cached HTML, not the database
- `loading="lazy"` on all deal images

---

## 10. Authentication

Authentication is optional — the site is fully usable without an account. Registration is incentivised by user-only features: watchlist and email alerts.

### Social Login

Uses ASP.NET Core Identity with cookie authentication and Google OAuth as the only social provider at MVP:

```
GET /auth/login/Google  → Challenge with Google OAuth
POST /auth/logout       → Sign out, redirect to /
```

Cookie settings:
- Scheme: `Cookies`
- Expiry: 30 days
- Sliding expiration: enabled

### Adding Apple Sign In (Phase 2)

The `appsettings.json` already has the Apple configuration keys stubbed out. When you obtain Apple developer credentials, add the `.AddApple(...)` call to `Program.cs` and fill in the environment variables.

### Hangfire Dashboard Security

`/admin/jobs` is protected by `HangfireAdminAuthFilter`:
- **Development**: open to everyone
- **Production**: requires `IsAuthenticated && IsInRole("Admin")`

---

## 11. Caching Strategy

Two layers of caching work together: .NET 10 Output Cache (server-side) and Cloudflare (edge).

### Output Cache Policies

| Policy Name | TTL | Varies By | Cache Tag |
|---|---|---|---|
| Base (default) | 5 min | — | — |
| `DealPage` | 30 min | `slug` route value | `deals` |
| `CategoryPage` | 10 min | `categorySlug` route value | `categories` |

### Cache Invalidation

Cache tags allow targeted invalidation. When a deal is created or expired, you can invalidate the `deals` tag to force a cache refresh without clearing all cached pages:

```csharp
await outputCacheStore.EvictByTagAsync("deals", cancellationToken);
```

### Cloudflare Edge Caching

Cloudflare sits in front of the Hetzner VPS and caches responses at the edge (geographically close to the user). Configure a Cloudflare Cache Rule to respect the `Cache-Control` headers emitted by .NET Output Cache. This means most page loads never reach your server at all.

---

## 12. Background Jobs

Hangfire is backed by PostgreSQL — no separate job queue infrastructure needed.

### Registered Jobs

| Job ID | Schedule | Handler | Purpose |
|---|---|---|---|
| `deal-url-health-check` | Every 6 hours | `DealUrlHealthCheckJob` | HTTP HEAD check on active deal URLs; expires dead links |
| `daily-digest` | Daily 8 AM UTC | `DailyDigestJob` | Sends batched digest emails for digest-mode users |
| `deal-score-recalculation` | Every 2 hours | `DealScoreRecalculationJob` | Recalculates score for all active deals |

### Hangfire Dashboard

Available at `/admin/jobs`. Shows job history, retry queues, and scheduled jobs. Restricted to admin role in production.

### Adding New Jobs

Register recurring jobs in `DependencyInjection.UseInfrastructure()`:

```csharp
RecurringJob.AddOrUpdate<MyJob>(
    "my-job-id",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily());
```

---

## 13. Email Notifications

Email is sent via the Resend API (`IEmailService` → `ResendEmailService`). Free tier: 3,000 emails/month.

### Three Trigger Types (MVP)

| Trigger | When Sent | User Setting |
|---|---|---|
| New deal in followed category | When a new deal is posted in a category the user watches | `NewDealInCategory` |
| Watchlist keyword match | When a new deal title matches a keyword the user watches | `WatchlistMatch` |
| Deal expiring soon | When a deal is about to expire | `DealExpiringSoon` (off by default) |

### Notification Frequency

Users choose per-tenant:
- **Instant** — email sent immediately when trigger fires
- **Daily Digest** — all triggers for the day batched into one email

### Email Configuration

```json
"Email": {
  "ResendApiKey": "re_...",
  "FromAddress": "deals@yourdomain.com",
  "FromName": "DealSpot"
}
```

---

## 14. Deal Score

A composite quality score (0–100) shown as a badge on every deal card and detail page.

### Score Components

| Component | Weight | Source |
|---|---|---|
| Discount depth | ~40% | `DealPrice.DiscountPercentage` — how far below MSRP |
| Retailer trust | ~30% | Lookup by `RetailerName` — Amazon/Best Buy score higher than unknown sellers |
| Community velocity | ~30% | Upvote rate in first 30 minutes (Phase 2) |

### Score Labels

| Score | Label |
|---|---|
| 85–100 | 🔥 Excellent |
| 70–84 | 👍 Great |
| 50–69 | ✅ Good |
| 30–49 | 😐 Fair |
| 0–29 | ⚠️ Weak |

### Current State

`DealScoreCalculator` (static class in `Application/Deals/Score/`) computes scores using a weighted formula: 40% discount depth, 30% retailer trust, 30% vote velocity. Initial score is calculated on deal creation via `DealCreatedScoreHandler`. A Hangfire job (`DealScoreRecalculationJob`, every 2h) recalculates all active deals to reflect vote changes. Price history signal is planned for a future phase.

---

## 15. Database

### Connection String

```
Host=localhost;Port=5432;Database=dealspot;Username=dealspot;Password=...
```

In Docker Compose production: `Host=postgres` (service name).

### Core Tables

| Table | Key Columns | Notes |
|---|---|---|
| `tenants` | `id`, `slug`, `domain`, `is_active`, `branding` (JSONB) | One row per niche site |
| `categories` | `id`, `tenant_id`, `name`, `slug`, `parent_id` | Tenant-scoped tree |
| `deals` | `id`, `tenant_id`, `category_id`, `slug`, `status`, `current_price`, `original_price`, `affiliate_url`, `original_url` | Core aggregate |
| `affiliate_networks` | `id`, `tenant_id`, `retailer_domain`, `affiliate_tag`, `url_template` | URL rewriting rules |
| `app_users` | `id`, `email`, `display_name`, `reputation_score` | Domain user entity |
| `watchlist_entries` | `id`, `user_id`, `tenant_id`, `category_id`, `product_keyword`, `target_price` | User watchlist |
| `notification_preferences` | `id`, `user_id`, `tenant_id`, `frequency` | Per-user email settings |
| `price_snapshots` | `id`, `deal_id`, `price`, `original_price`, `currency`, `source`, `recorded_at_utc` | Price history tracking |

Plus ASP.NET Core Identity tables: `AspNetUsers`, `AspNetRoles`, etc.

### Key Indexes

```sql
-- Deal feed queries
CREATE INDEX idx_deals_tenant_status ON deals (tenant_id, status);
CREATE INDEX idx_deals_tenant_category_status ON deals (tenant_id, category_id, status);

-- Slug lookup for deal detail pages
CREATE UNIQUE INDEX idx_deals_tenant_slug ON deals (tenant_id, slug);

-- Time-ordered feed
CREATE INDEX idx_deals_created_at ON deals (created_at_utc);
```

### Migrations

Using EF Core migrations. Generate after any model change:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DealSpot.Infrastructure \
  --startup-project src/DealSpot.Web

dotnet ef database update \
  --project src/DealSpot.Infrastructure \
  --startup-project src/DealSpot.Web
```

---

## 16. Environment Configuration

All secrets are loaded from environment variables. Never commit secrets to source control.

### Required Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string (if used) |
| `Auth__Google__ClientId` | Google OAuth client ID |
| `Auth__Google__ClientSecret` | Google OAuth client secret |
| `Email__ResendApiKey` | Resend API key (`re_...`) |
| `Email__FromAddress` | Sender email address |
| `Email__FromName` | Sender display name |
| `POSTGRES_PASSWORD` | PostgreSQL password (Docker Compose) |
| `REDIS_PASSWORD` | Redis password (Docker Compose) |

### Setup

```bash
cd docker
cp .env.example .env
# Open .env and fill in all values
```

---

## 17. Local Development Setup

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- A Google Cloud project with OAuth 2.0 credentials (for auth testing)
- A Resend account (free tier)

### Steps

```bash
# 1. Clone and navigate
git clone https://github.com/you/dealspot.git
cd dealspot

# 2. Configure secrets
cd docker
cp .env.example .env
# Edit .env

# 3. Start PostgreSQL + Redis
docker compose up postgres redis -d

# 4. Create database schema
cd ..
dotnet ef database update \
  --project src/DealSpot.Infrastructure \
  --startup-project src/DealSpot.Web

# 5. Seed a tenant (see Section 19)

# 6. Run the app
dotnet run --project src/DealSpot.Web
# Open https://localhost:5001
```

### Local Subdomain Routing

Since subdomains require DNS, add entries to your hosts file for local testing:

**Windows:** `C:\Windows\System32\drivers\etc\hosts`
```
127.0.0.1  tech.localhost
127.0.0.1  aquarium.localhost
```

Insert tenant rows with `domain = 'tech.localhost'` and browse to `https://tech.localhost:5001`.

---

## 18. Production Deployment

### Infrastructure

- **Server:** Hetzner CX22 — 2 vCPU, 4 GB RAM, ~€4/month
- **OS:** Ubuntu 24.04
- **DNS + CDN:** Cloudflare (free tier)

### Initial Server Setup

```bash
# Install Docker
curl -fsSL https://get.docker.com | sh

# Clone repo
git clone https://github.com/you/dealspot.git
cd dealspot

# Configure secrets
cp docker/.env.example docker/.env
nano docker/.env  # fill in all values

# Start everything
docker compose -f docker/docker-compose.yml up -d

# Apply migrations
docker compose exec web dotnet ef database update \
  --project src/DealSpot.Infrastructure \
  --startup-project src/DealSpot.Web
```

### Cloudflare DNS Setup

For each niche subdomain, add an **A record** in Cloudflare:

| Type | Name | Value | Proxy |
|---|---|---|---|
| A | tech | `<Hetzner IP>` | ✅ Proxied |
| A | aquarium | `<Hetzner IP>` | ✅ Proxied |
| A | supplements | `<Hetzner IP>` | ✅ Proxied |

Cloudflare handles SSL automatically via its proxy. Set SSL/TLS mode to **Full (strict)**.

### Updating the App

```bash
cd dealspot
git pull
docker compose -f docker/docker-compose.yml build web
docker compose -f docker/docker-compose.yml up -d web
```

### Monitoring

- **Health check endpoint:** `https://tech.yourdomain.com/health`
- **Hangfire dashboard:** `https://tech.yourdomain.com/admin/jobs` (admin login required)
- **Logs:** `docker compose logs web -f` or files in `logs/` directory

---

## 19. Adding a New Tenant

No code change or redeployment is required. Insert a row and add a DNS record.

```sql
INSERT INTO tenants (id, slug, name, domain, is_active, created_at_utc, branding)
VALUES (
  gen_random_uuid(),
  'aquarium',
  'AquaDeals',
  'aquarium.yourdomain.com',
  true,
  now(),
  '{
    "primaryColor": "#0077b6",
    "secondaryColor": "#023e8a",
    "logoUrl": "/images/aqua-logo.svg",
    "faviconUrl": "/images/aqua-favicon.ico",
    "metaDescription": "Best deals on aquarium supplies — filters, lighting, live plants and more.",
    "metaKeywords": "aquarium deals, fish tank deals, filter deals, aquarium lights"
  }'
);
```

Then add categories for that tenant:

```sql
-- Get the new tenant's ID
WITH t AS (SELECT id FROM tenants WHERE slug = 'aquarium')
INSERT INTO categories (id, tenant_id, name, slug, parent_id, is_active, sort_order)
VALUES
  (gen_random_uuid(), (SELECT id FROM t), 'Filters',   'filters',   NULL, true, 1),
  (gen_random_uuid(), (SELECT id FROM t), 'Lighting',  'lighting',  NULL, true, 2),
  (gen_random_uuid(), (SELECT id FROM t), 'Live Stock', 'live-stock', NULL, true, 3),
  (gen_random_uuid(), (SELECT id FROM t), 'Equipment', 'equipment', NULL, true, 4);
```

---

## 20. Adding Affiliate Networks

```sql
-- Amazon Associates (works across all Amazon product pages)
INSERT INTO affiliate_networks (id, tenant_id, retailer_domain, network_name, affiliate_tag, url_template, is_active)
VALUES (
  gen_random_uuid(),
  (SELECT id FROM tenants WHERE slug = 'tech'),
  'amazon.com',
  'Amazon Associates',
  'techdeals-20',        -- your Amazon associate tag
  '{url}?tag={tag}',
  true
);

-- Best Buy via Impact
INSERT INTO affiliate_networks (id, tenant_id, retailer_domain, network_name, affiliate_tag, url_template, is_active)
VALUES (
  gen_random_uuid(),
  (SELECT id FROM tenants WHERE slug = 'tech'),
  'bestbuy.com',
  'Impact',
  'your-impact-tag',
  '{url}&affid={tag}',
  true
);
```

**Note:** The `url_template` format varies by affiliate network. Check your network's documentation for the correct parameter format.

---

## 21. Testing

### Test Projects

| Project | Tests | Framework |
|---|---|---|
| `DealSpot.Domain.Tests` | Domain entity behaviour, value object calculations, domain event emission | xUnit + FluentAssertions |
| `DealSpot.Application.Tests` | Command handler success/failure paths, repository interaction verification | xUnit + NSubstitute + FluentAssertions |

### Running Tests

```bash
dotnet test
```

### Domain Test Examples

```csharp
// Deal auto-expires at 5 community reports
[Fact]
public void ReportAsExpired_AtFiveReports_ShouldAutoExpire()

// Reporting an already-expired deal returns a failure Result
[Fact]
public void ReportAsExpired_WhenAlreadyExpired_ShouldReturnFailure()

// DealCreatedDomainEvent is raised on successful creation
[Fact]
public void Create_ShouldRaiseDealCreatedDomainEvent()
```

### Application Test Examples

```csharp
// Happy path: deal is created, affiliate URL is rewritten, repository is called
[Fact]
public async Task Handle_WithValidCommand_ShouldCreateDealAndReturnResponse()

// Category from a different tenant is rejected
[Fact]
public async Task Handle_WhenCategoryBelongsToDifferentTenant_ShouldReturnFailure()
```

### Test Philosophy

- Domain tests are pure unit tests — no mocks, no DI, just `new` and assertions
- Application tests mock all infrastructure interfaces with NSubstitute
- Infrastructure tests (integration) are a Phase 2 addition using Testcontainers for a real PostgreSQL instance

---

## 22. Monetization Strategy

### Tier 1 — Affiliate Commissions (Live from Day 1)

Every outbound click on "Get This Deal" goes through your affiliate link. Revenue is earned when the user completes a purchase.

| Network | Commission Range | Sign-up |
|---|---|---|
| Amazon Associates | 1–10% depending on category | affiliate-program.amazon.com |
| Best Buy Affiliate | ~1% | via Impact |
| CJ Affiliate | Varies | cj.com |
| ShareASale | Varies | shareasale.com |

The URL rewriting pipeline is already live — you just need to register with each network and insert rows into `affiliate_networks`.

### Tier 2 — Display Advertising (Phase 2)

Google AdSense integration. Expected RPM at launch: $1–3. Grows significantly with traffic volume. Placement strategy: sidebar and below-deal-description — never interstitials that interrupt the deal click.

### Tier 3 — Sponsored Placements (Phase 3)

Once you have an established audience, brands pay for featured placement at the top of category pages or in deal digests. Requires a media kit and a minimum traffic threshold.

### Cost Breakdown (MVP, monthly)

| Service | Cost |
|---|---|
| Hetzner CX22 VPS | ~€4 |
| Cloudflare | Free |
| Resend (up to 3,000 emails) | Free |
| Domain | ~$12/year |
| **Total** | **~€5–6/month** |

---

## 23. Development Roadmap

### Phase 1 — MVP (Current)
- [x] Multi-tenant Blazor SSR shell
- [x] Affiliate URL auto-rewriting pipeline
- [x] Deal creation with SEO slug generation
- [x] Dedicated deal pages with Product + BreadcrumbList JSON-LD
- [x] ItemList JSON-LD on home page
- [x] Open Graph tags
- [x] .NET 10 Output Caching (3 policies)
- [x] Community expired deal reporting (auto-expires at 5 reports)
- [x] Hangfire URL health check job (every 6 hours)
- [x] Docker Compose production deployment
- [x] Health check endpoint

### Phase 2 — Community (complete)
- [x] Google + Apple social login
- [x] Deal watchlist (category watch + keyword watch)
- [x] Email alerts via Resend (3 trigger types: new deal, watchlist match, expiring soon)
- [x] Daily digest email option
- [x] Deal Score calculation (discount + retailer trust + vote velocity)
- [x] Upvote / downvote on deals
- [x] AdSense integration
- [x] Admin deal submission UI
- [x] `DealScoreCalculatorJob` background job
- [x] Category browse pages
- [x] Output cache invalidation via cache tags

### Phase 3 — Intelligence (in progress)
- [x] Price history — admin price updates recorded as snapshots, bar chart on deal detail
- [x] Price drop alerts — watchlist + price change triggers, instant + digest delivery
- [ ] Price history — Keepa API for Amazon products (price scraping job)
- [ ] Price history — custom HTTP scraper for niche retailers
- [ ] Full Deal Score incorporating price history signal
- [ ] Community deal submissions with approval queue
- [ ] User reputation system
- [ ] Browser push notifications (Web Push / VAPID)

---

## 24. Key Architectural Decisions

These decisions were made deliberately during the design phase. Understanding the reasoning helps when extending the system.

### Single deployment, shared database with TenantId

**Decision:** One .NET app, one PostgreSQL database, `TenantId` on every core table.

**Reasoning:** Three separate deployments would cost 3× as much (~€15/month vs €5/month), require maintaining three separate CI pipelines, and multiply all operational overhead. TenantId isolation achieves the same data separation at a fraction of the cost. Extracting a high-traffic tenant to its own database later is a straightforward migration using TenantId as the partition key.

### Blazor SSR over React/Next.js

**Decision:** .NET 10 Blazor Server-Side Rendering for the frontend.

**Reasoning:** A deal site is SEO-critical — pages must be server-rendered for Google to index deal titles, prices, and product names. Blazor United (Static SSR + selective Interactive Server) provides this while keeping the entire stack in C#. Switching to Next.js would mean context-switching between C# and TypeScript, two separate deployment pipelines, and losing tight integration with the .NET backend.

### Affiliate URL rewriting at the application layer

**Decision:** Affiliate tags are injected automatically in `CreateDealCommandHandler`, not by the submitter.

**Reasoning:** This ensures 100% of deal URLs — including future community submissions — earn affiliate revenue without requiring submitters to know or care about affiliate programs. Storing the clean `OriginalUrl` separately enables price history tracking without affiliate parameters polluting the canonical URL.

### PostgreSQL over SQL Server

**Decision:** PostgreSQL 17.

**Reasoning:** Free, first-class EF Core support via Npgsql, JSONB for flexible tenant branding config, built-in full-text search for deal search (Phase 2), and TimescaleDB extension for price history time-series data (Phase 3). SQL Server would add significant cost with no meaningful benefit for this use case.

### Hetzner + Docker Compose over managed cloud

**Decision:** €4/month Hetzner VPS with Docker Compose over Azure App Service or Railway.

**Reasoning:** At MVP scale (one person, low traffic), managed cloud services cost 3–10× more for equivalent compute. Docker Compose provides full control and a clear migration path to Kubernetes if/when needed. Cloudflare in front of Hetzner absorbs traffic spikes and provides free SSL, DDoS protection, and global edge caching.

### Optional user registration

**Decision:** The site works fully without an account. Registration is incentivised, not required.

**Reasoning:** Mandatory registration creates friction and causes bounce. At launch, SEO traffic lands on deal pages — forcing a sign-up before seeing the deal would kill conversion. Registered users unlock watchlist and alerts, which drives repeat visits and creates retention. This mirrors the Slickdeals model vs PZDeals (no accounts).

### Resend over SendGrid for email

**Decision:** Resend as the email provider.

**Reasoning:** Resend provides 3,000 free emails/month vs SendGrid's 100/day free tier. The Resend .NET SDK is clean and well-maintained. At MVP scale of email volume, Resend is effectively free.
