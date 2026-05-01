# DealSpot

Multi-tenant deal aggregator platform built with .NET 10 Blazor SSR, PostgreSQL, and Redis.

## Architecture

```
DealSpot/
+-- src/
¦   +-- DealSpot.Domain/          # Entities, Value Objects, Domain Events, Repository interfaces
¦   +-- DealSpot.Application/     # CQRS Commands/Queries, MediatR, FluentValidation
¦   +-- DealSpot.Infrastructure/  # EF Core, Repositories, Hangfire, Email, Affiliate rewriting
¦   +-- DealSpot.Web/             # Blazor SSR (.NET 10), Output Caching, Auth
¦   +-- DealSpot.AppHost/         # .NET Aspire orchestrator (local dev)
¦   +-- DealSpot.ServiceDefaults/ # Shared telemetry, health checks, service discovery
+-- tests/
¦   +-- DealSpot.Domain.Tests/
¦   +-- DealSpot.Application.Tests/
+-- docker/
    +-- Dockerfile
    +-- docker-compose.yml
    +-- .env.example
```

## Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 10 Blazor SSR |
| Database | PostgreSQL 17 |
| ORM | EF Core 10 + Dapper (reads) |
| Background Jobs | Hangfire |
| Email | Resend |
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

Or in Visual Studio: right-click `DealSpot.AppHost` ? Manage User Secrets, then add:

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

**3. Apply EF Core migrations**

Stop the AppHost (Shift+F5), then run from the solution root:

```cmd
"%USERPROFILE%\.dotnet\tools\dotnet-ef.exe" database update ^
  --project src\DealSpot.Infrastructure ^
  --startup-project src\DealSpot.Web ^
  --connection "Host=localhost;Port=5432;Database=dealspot;Username=postgres;Password=your-password-here"
```

If `dotnet-ef` is not installed:

```cmd
"C:\Program Files\dotnet\dotnet.exe" tool install --global dotnet-ef
```

**4. Seed a dev tenant**

Open pgAdmin (link in Aspire dashboard) and run:

```sql
INSERT INTO tenants (id, slug, name, domain, is_active, created_at_utc, branding)
VALUES (
  'a0000000-0000-0000-0000-000000000001',
  'localhost',
  'DealSpot Dev',
  'localhost',
  true,
  NOW(),
  '{"PrimaryColor":"#3B82F6","SecondaryColor":"#1E40AF","LogoUrl":"","FaviconUrl":"","MetaDescription":"DealSpot Dev","MetaKeywords":"deals"}'
);
```

**5. Start the AppHost again and browse to the app URL shown in the dashboard.**

---

### Adding subsequent migrations

```cmd
"%USERPROFILE%\.dotnet\tools\dotnet-ef.exe" migrations add MigrationName ^
  --project src\DealSpot.Infrastructure ^
  --startup-project src\DealSpot.Web ^
  --output-dir Persistence/Migrations
```

Then apply with `database update` as in step 3.

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
- `tech.yourdomain.com` ? Hetzner IP
- `aquarium.yourdomain.com` ? same IP

## MVP Roadmap

### Phase 1 — Core (you as sole submitter)
- [x] Multi-tenant Blazor SSR shell
- [x] Deal submission with affiliate URL auto-rewriting
- [x] Dedicated SEO pages with JSON-LD schema (Product + Offer + BreadcrumbList)
- [x] .NET 10 Output Caching (Redis) + Cloudflare edge caching
- [x] Community expired deal reporting (auto-expires at 5 reports)
- [x] Hangfire URL health check job (every 6 hours)
- [x] .NET Aspire local dev orchestration (PostgreSQL + Redis + pgAdmin + dashboard)
- [x] EF Core migrations
- [x] Docker Compose deployment

### Phase 2 — Community
- [ ] Google + Apple social login
- [ ] Deal watchlist + email alerts via Resend (3 trigger types)
- [ ] Deal Score (discount % + retailer trust + vote velocity)
- [ ] AdSense integration

### Phase 3 — Intelligence
- [ ] Price history (Keepa API for Amazon + custom scrapers)
- [ ] Full Deal Score incorporating price history
- [ ] Community deal submissions + approval queue
- [ ] Upvote/downvote system
- [ ] User reputation system
- [ ] Browser push notifications
