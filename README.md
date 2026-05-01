# DealSpot

Multi-tenant deal aggregator platform built with .NET 10 Blazor SSR, PostgreSQL, and Docker.

## Architecture

```
DealSpot/
├── src/
│   ├── DealSpot.Domain/          # Entities, Value Objects, Domain Events, Repository interfaces
│   ├── DealSpot.Application/     # CQRS Commands/Queries, MediatR, FluentValidation
│   ├── DealSpot.Infrastructure/  # EF Core, Repositories, Hangfire, Email, Affiliate rewriting
│   └── DealSpot.Web/             # Blazor SSR (.NET 10), Output Caching, Auth
├── tests/
│   ├── DealSpot.Domain.Tests/
│   └── DealSpot.Application.Tests/
└── docker/
    ├── Dockerfile
    ├── docker-compose.yml
    └── .env.example
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
| Caching | .NET 10 Output Cache |
| CDN / Proxy | Cloudflare (free) |
| Hosting | Hetzner VPS + Docker Compose |

## Local Development

```bash
# 1. Start infrastructure
cd docker
cp .env.example .env
# Edit .env with your credentials
docker compose up postgres redis -d

# 2. Apply EF Core migrations
dotnet ef database update \
  --project src/DealSpot.Infrastructure \
  --startup-project src/DealSpot.Web

# 3. Run the app
dotnet run --project src/DealSpot.Web
```

## Adding a Tenant

```sql
INSERT INTO tenants (id, slug, name, domain, is_active, created_at_utc, branding)
VALUES (
  gen_random_uuid(), 'tech', 'TechDeals', 'tech.yourdomain.com', true, now(),
  '{
    "primaryColor": "#e63946",
    "secondaryColor": "#1d3557",
    "logoUrl": "/images/tech-logo.svg",
    "faviconUrl": "/favicon.ico",
    "metaDescription": "Best tech deals today.",
    "metaKeywords": "tech deals, laptop deals, GPU deals"
  }'
);
```

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
- `tech.yourdomain.com` → Hetzner IP
- `aquarium.yourdomain.com` → same IP
- `supplements.yourdomain.com` → same IP

## MVP Roadmap

### Phase 1 — Core (you as sole submitter)
- [x] Multi-tenant Blazor SSR shell
- [x] Deal submission with affiliate URL auto-rewriting
- [x] Dedicated SEO pages with JSON-LD schema (Product + Offer + BreadcrumbList)
- [x] .NET 10 Output Caching + Cloudflare edge caching
- [x] Community expired deal reporting (auto-expires at 5 reports)
- [x] Hangfire URL health check job (every 6 hours)
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
