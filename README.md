# DealSpot

Multi-tenant deal aggregator platform built with .NET 10 Blazor SSR, PostgreSQL, and Redis.

## Architecture

```
DealSpot/
src/
  DealSpot.Domain/            # Entities, Value Objects, Domain Events, Repository interfaces
  DealSpot.Application/       # CQRS Commands/Queries, MediatR, FluentValidation
  DealSpot.Infrastructure/    # EF Core, Repositories, Hangfire, Email, Affiliate rewriting
  DealSpot.Web/               # Blazor SSR (.NET 10), Output Caching, Auth
  DealSpot.AppHost/           # .NET Aspire orchestrator (local dev)
  DealSpot.ServiceDefaults/   # Shared telemetry, health checks, service discovery
tests/
  DealSpot.Domain.Tests/      # 10 domain unit tests
  DealSpot.Application.Tests/ # 54 application unit tests
specs/                        # Feature specs (one per feature)
docker/
  Dockerfile
  docker-compose.yml
  .env.example
```
## Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 10 Blazor SSR |
| Database | PostgreSQL 17 |
| ORM | EF Core 10 + Dapper (reads) |
| Background Jobs | Hangfire |
| Email | Resend |
| Push Notifications | WebPush NuGet (VAPID) |
| Auth | ASP.NET Core Identity + Google OAuth |
| Caching | Redis (Output Cache via Aspire) |
| Local Orchestration | .NET Aspire 13 |
| Observability | OpenTelemetry + Aspire Dashboard |
| CDN / Proxy | Cloudflare (free) |
| Hosting | Hetzner VPS + Docker Compose |

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Visual Studio 2022 17.x or Rider

### First-time setup

**1. Set the PostgreSQL password in AppHost user secrets**

The AppHost uses a pinned password so the persistent volume never gets mismatched. Set it once:

```
cd C:\path\to\DealSpot
"%USERPROFILE%\.dotnet\tools\dotnet-ef.exe" (see step 3)
```

Or in Visual Studio: right-click `DealSpot.AppHost` > Manage User Secrets, then add:

```json
{
  "Parameters:pg-password": "your-password-here"
}
```

Alternatively the password is also read from `src/DealSpot.AppHost/appsettings.Development.json` (gitignored):

```json
{
  "Parameters": {
    "pg-password": "your-password-here"
  }
}
```

**2. Run the AppHost**

Set `DealSpot.AppHost` as the startup project and press F5. This will:
- Pull and start a PostgreSQL 17 container on `localhost:5432` (fixed port)
- Pull and start a Redis container
- Launch the Aspire dashboard (opens automatically in browser)
- Start `DealSpot.Web`

**3. Migrations run automatically**

EF Core migrations run automatically when the web app starts (via `UseInfrastructure()`). The AppHost's `.WaitFor(db)` ensures PostgreSQL is ready before the app boots. No manual `dotnet ef database update` needed for local dev.

To **add a new migration** (after changing entities/configurations):

```cmd
"%USERPROFILE%\.dotnet\.tools\dotnet-ef.exe" migrations add MigrationName ^
  --project src\DealSpot.Infrastructure ^
  --startup-project src\DealSpot.Web ^
  --output-dir Persistence/Migrations
```

The next app startup will apply it automatically.

**4. Seed data loads automatically**

`SeedDevelopmentData.cs` runs on first startup in Development mode. It creates a "TechDeals" tenant (domain: `localhost`), 6 categories, 3 users, 11 deals, votes, watchlist entries, price snapshots, and notification preferences. Skips automatically if tenants already exist.

To reset: drop the database (`docker rm -f <pg-container>` and remove the volume) and restart.

**5. Start the AppHost and browse to the app URL shown in the Aspire dashboard.**

---

### Adding subsequent migrations

```cmd
"%USERPROFILE%\.dotnet\tools\dotnet-ef.exe" migrations add MigrationName ^
  --project src\DealSpot.Infrastructure ^
  --startup-project src\DealSpot.Web ^
  --output-dir Persistence/Migrations
```

The migration will be applied automatically on the next app startup. No manual `database update` needed.

---

## Adding a Tenant

```sql
INSERT INTO tenants (id, slug, name, domain, is_active, created_at_utc, branding)
VALUES (
  gen_random_uuid(), 'tech', 'TechDeals', 'tech.yourdomain.com', true, NOW(),
  '{"PrimaryColor":"#e63946","SecondaryColor":"#1d3557","LogoUrl":"/images/tech-logo.svg","FaviconUrl":"/favicon.ico","MetaDescription":"Best tech deals today.","MetaKeywords":"tech deals, laptop deals"}'
);
```

> Tenant resolution is cached for 30 minutes. Restart the web app to pick up new tenants immediately.

## Adding Affiliate Networks

```sql
INSERT INTO affiliate_networks (id, tenant_id, retailer_domain, network_name, affiliate_tag, url_template, is_active)
VALUES
  (gen_random_uuid(), '<tenant_id>', 'amazon.com', 'Amazon Associates', 'yoursite-20', '{url}?tag={tag}', true),
  (gen_random_uuid(), '<tenant_id>', 'bestbuy.com', 'Impact', 'yoursite', '{url}&affid={tag}', true);
```

## Production Deployment (Hetzner VPS)

```bash
git clone https://github.com/you/dealspot.git && cd dealspot
cp docker/.env.example docker/.env
# Fill in production secrets
docker compose -f docker/docker-compose.yml up -d
```

Point Cloudflare DNS (proxied) for each subdomain to your Hetzner IP:
- `tech.yourdomain.com` -> Hetzner IP
- `aquarium.yourdomain.com` -> same IP

## Running Tests

```cmd
dotnet test tests/DealSpot.Application.Tests
dotnet test tests/DealSpot.Domain.Tests
```

64 tests total (54 application + 10 domain). xUnit + NSubstitute + FluentAssertions.

## Features

### Phase 1 -- Core (admin submits deals)
- [x] Multi-tenant Blazor SSR shell with subdomain resolution + per-tenant branding
- [x] Deal submission with affiliate URL auto-rewriting pipeline
- [x] Dedicated SEO pages with JSON-LD schema (Product + Offer + BreadcrumbList)
- [x] .NET 10 Output Caching (Redis) + Cloudflare edge caching
- [x] Community expired deal reporting (auto-expires at 5 reports)
- [x] Hangfire URL health check job (every 6 hours)
- [x] .NET Aspire local dev orchestration (PostgreSQL + Redis + pgAdmin + dashboard)
- [x] EF Core migrations
- [x] Docker Compose deployment

### Phase 2 -- Community & Engagement (complete)
- [x] Domain event dispatch (MediatR `IPublisher` after `SaveChangesAsync`)
- [x] Google OAuth login + domain `User` provisioning
- [x] Category browse pages (`/categories`, `/categories/{slug}`)
- [x] Output cache invalidation via domain event handlers (tag eviction)
- [x] Admin deal submission UI (policy-based auth, `/admin/deals`)
- [x] Deal watchlist -- category watches + keyword watches (`/watchlist`)
- [x] Deal voting -- upvote/downvote with denormalized counts, `VoteButton` component
- [x] Email alerts -- instant + daily digest via Resend, `DealCreatedNotificationHandler`
- [x] Notification preferences -- per-user toggles + frequency (`/settings/notifications`)
- [x] Deal Score calculation (discount % + retailer trust + vote velocity)
- [x] AdSense integration

### Phase 3 -- Intelligence (complete)
- [x] Price history tracking + admin price update UI + bar chart on deal detail
- [x] Price drop alerts (watchlist + price change triggers)
- [x] Community deal submissions + approval queue (`/submit` + admin approve/reject)
- [x] User reputation system — +10 approve, -2 reject, ±1 vote, -3 expire; auto-approve at ≥50 rep
- [x] Web push notifications (VAPID) — service worker, push toggle, 410 subscription pruning