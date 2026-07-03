using DealSpot.Domain.AffiliateLinks;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DealSpot.Infrastructure.Persistence;

// ponytail: dev-only seed. All deals saved as PendingApproval (no domain events), then status flipped via SQL.
public static class SeedDevelopmentData
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Tenants.AnyAsync())
        {
            logger.LogInformation("Seed data already exists, skipping");
            return;
        }

        logger.LogInformation("Seeding development data...");

        // ── Tenant ──────────────────────────────────────────────────────────
        var tenant = Tenant.Create("techdeals", "TechDeals", "localhost",
            new TenantBranding("#2563eb", "#1e40af", "/img/logo.svg", "/favicon.ico",
                "Best tech deals, curated daily", "tech,deals,discounts,electronics")).Value;
        db.Tenants.Add(tenant);
        var tenantId = tenant.Id;

        // ── Categories ──────────────────────────────────────────────────────
        var headphones = Category.Create(tenantId, "Headphones", "headphones").Value;
        var laptops = Category.Create(tenantId, "Laptops", "laptops").Value;
        var monitors = Category.Create(tenantId, "Monitors", "monitors").Value;
        var storage = Category.Create(tenantId, "Storage", "storage").Value;
        var peripherals = Category.Create(tenantId, "Peripherals", "peripherals").Value;
        var gaming = Category.Create(tenantId, "Gaming", "gaming").Value;
        db.Categories.AddRange(headphones, laptops, monitors, storage, peripherals, gaming);

        // ── Affiliate Network ───────────────────────────────────────────────
        db.AffiliateNetworks.Add(
            AffiliateNetwork.Create(tenantId, "amazon.com", "Amazon Associates", "techdeals-20", "{url}?tag={tag}"));

        // ── Users ───────────────────────────────────────────────────────────
        var admin = User.Create("admin@techdeals.dev", "Admin", "https://i.pravatar.cc/150?u=admin").Value;
        var alice = User.Create("alice@example.com", "Alice Chen", "https://i.pravatar.cc/150?u=alice").Value;
        var bob = User.Create("bob@example.com", "Bob Martinez", "https://i.pravatar.cc/150?u=bob").Value;
        db.AppUsers.AddRange(admin, alice, bob);

        // ── Deals — ALL as PendingApproval to avoid domain events ───────────
        var d1 = MakeDeal(tenantId, headphones.Id, admin.Id,
            "Sony WH-1000XM5 — All-Time Low", "sony-wh-1000xm5-all-time-low-278",
            "Industry-leading noise cancelling headphones at their lowest price ever.",
            278m, 399.99m, "https://amazon.com/dp/B0BX2L8SPG", "https://amazon.com/dp/B0BX2L8SPG?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/51aXvjzcukL._AC_SL1500_.jpg", "Amazon");

        var d2 = MakeDeal(tenantId, laptops.Id, admin.Id,
            "MacBook Air M3 15\" — $200 Off", "macbook-air-m3-15-200-off-1099",
            "Apple MacBook Air with M3 chip, 15.3\" Liquid Retina display, 16GB RAM, 256GB SSD.",
            1099m, 1299m, "https://amazon.com/dp/B0CX23V2ZK", "https://amazon.com/dp/B0CX23V2ZK?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/71f5Eu5lJSL._AC_SL1500_.jpg", "Amazon");

        var d3 = MakeDeal(tenantId, monitors.Id, admin.Id,
            "LG 27\" 4K IPS Monitor — 34% Off", "lg-27-4k-ips-monitor-34-off-229",
            "LG 27UL500-W 27\" UHD 4K IPS monitor with HDR10, 60Hz, FreeSync.",
            229.99m, 349.99m, "https://amazon.com/dp/B07PGL2WVS", "https://amazon.com/dp/B07PGL2WVS?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/71OEhNTh4eL._AC_SL1500_.jpg", "Amazon");

        var d4 = MakeDeal(tenantId, storage.Id, alice.Id,
            "Samsung 990 EVO 2TB NVMe SSD — $139", "samsung-990-evo-2tb-nvme-ssd-139",
            "PCIe 5.0 x2, sequential reads up to 5,000 MB/s. Great for gaming or content creation.",
            139m, 189.99m, "https://amazon.com/dp/B0CXKGBWRP", "https://amazon.com/dp/B0CXKGBWRP?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/51GVXjHI3eL._AC_SL1500_.jpg", "Amazon");

        var d5 = MakeDeal(tenantId, peripherals.Id, alice.Id,
            "Logitech MX Master 3S — $79", "logitech-mx-master-3s-79",
            "Wireless performance mouse with 8K DPI, quiet clicks, MagSpeed scroll.",
            79.99m, 99.99m, "https://amazon.com/dp/B09HM94VDS", "https://amazon.com/dp/B09HM94VDS?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/61ni3t1ryQL._AC_SL1500_.jpg", "Amazon");

        var d6 = MakeDeal(tenantId, gaming.Id, bob.Id,
            "PS5 DualSense Controller — $49", "ps5-dualsense-controller-49",
            "Sony PlayStation 5 DualSense wireless controller. Haptic feedback, adaptive triggers.",
            49m, 69.99m, "https://amazon.com/dp/B0CQXC5MNX", "https://amazon.com/dp/B0CQXC5MNX?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/61lsFMOxHiL._SL1500_.jpg", "Amazon");

        var d7 = MakeDeal(tenantId, headphones.Id, bob.Id,
            "AirPods Pro 2 — $179", "airpods-pro-2-179",
            "Apple AirPods Pro 2nd gen with USB-C, active noise cancellation, adaptive transparency.",
            179m, 249m, "https://amazon.com/dp/B0CHWRXH8B", "https://amazon.com/dp/B0CHWRXH8B?tag=techdeals-20",
            "https://m.media-amazon.com/images/I/61SUj2aKoEL._AC_SL1500_.jpg", "Amazon");

        var pending1 = MakeDeal(tenantId, laptops.Id, alice.Id,
            "ThinkPad X1 Carbon Gen 12 — $1,149", "thinkpad-x1-carbon-gen-12-1149",
            "Lenovo ThinkPad X1 Carbon, Intel Ultra 7, 16GB, 512GB SSD.",
            1149m, 1499m, "https://lenovo.com/x1carbon", "https://lenovo.com/x1carbon", null, "Lenovo");

        var pending2 = MakeDeal(tenantId, gaming.Id, bob.Id,
            "Xbox Elite Controller Series 2 — $129", "xbox-elite-controller-series-2-129",
            "Microsoft Xbox Elite Wireless Controller Series 2.",
            129m, 179.99m, "https://amazon.com/dp/B07SFKTLZM", "https://amazon.com/dp/B07SFKTLZM?tag=techdeals-20", null, "Amazon");

        var expired = MakeDeal(tenantId, storage.Id, admin.Id,
            "WD Black SN850X 1TB — $69 (EXPIRED)", "wd-black-sn850x-1tb-69",
            "Was a great deal. No longer available at this price.",
            69m, 109.99m, "https://amazon.com/dp/B0B7CKVCCV", "https://amazon.com/dp/B0B7CKVCCV?tag=techdeals-20", null, "Amazon");

        var rejected = MakeDeal(tenantId, peripherals.Id, bob.Id,
            "No-Name USB Hub — $5", "no-name-usb-hub-5",
            "Generic USB hub, not really a noteworthy deal.",
            5m, 8m, "https://aliexpress.com/item/12345", "https://aliexpress.com/item/12345", null, "AliExpress");

        db.Deals.AddRange(d1, d2, d3, d4, d5, d6, d7, pending1, pending2, expired, rejected);

        // ── Votes ───────────────────────────────────────────────────────────
        d1.ApplyVote(1); d1.ApplyVote(1);
        d2.ApplyVote(1); d2.ApplyVote(-1);
        d3.ApplyVote(1);
        d4.ApplyVote(1); d4.ApplyVote(1);
        d5.ApplyVote(1);
        d6.ApplyVote(1); d6.ApplyVote(1);
        d7.ApplyVote(1);
        db.DealVotes.AddRange(
            DealVote.Create(d1.Id, alice.Id, 1), DealVote.Create(d1.Id, bob.Id, 1),
            DealVote.Create(d2.Id, alice.Id, 1), DealVote.Create(d2.Id, bob.Id, -1),
            DealVote.Create(d3.Id, bob.Id, 1),
            DealVote.Create(d4.Id, admin.Id, 1), DealVote.Create(d4.Id, bob.Id, 1),
            DealVote.Create(d5.Id, admin.Id, 1),
            DealVote.Create(d6.Id, admin.Id, 1), DealVote.Create(d6.Id, alice.Id, 1),
            DealVote.Create(d7.Id, alice.Id, 1));

        // ── Watchlist ───────────────────────────────────────────────────────
        db.WatchlistEntries.AddRange(
            WatchlistEntry.CreateCategoryWatch(alice.Id, tenantId, headphones.Id),
            WatchlistEntry.CreateCategoryWatch(alice.Id, tenantId, laptops.Id),
            WatchlistEntry.CreateKeywordWatch(bob.Id, tenantId, "Sony", 200m),
            WatchlistEntry.CreateKeywordWatch(bob.Id, tenantId, "SSD"),
            WatchlistEntry.CreateCategoryWatch(admin.Id, tenantId, gaming.Id));

        // ── Price Snapshots ─────────────────────────────────────────────────
        var now = DateTime.UtcNow;
        db.PriceSnapshots.AddRange(
            MakeSnapshot(d1.Id, 278m, 399.99m, "initial", now),
            MakeSnapshot(d2.Id, 1099m, 1299m, "initial", now),
            MakeSnapshot(d3.Id, 229.99m, 349.99m, "initial", now),
            MakeSnapshot(d4.Id, 139m, 189.99m, "initial", now),
            MakeSnapshot(d5.Id, 79.99m, 99.99m, "initial", now),
            MakeSnapshot(d6.Id, 49m, 69.99m, "initial", now),
            MakeSnapshot(d7.Id, 179m, 249m, "initial", now),
            MakeSnapshot(d1.Id, 348m, 399.99m, "admin_update", now.AddDays(-14)),
            MakeSnapshot(d1.Id, 318m, 399.99m, "admin_update", now.AddDays(-7)),
            MakeSnapshot(d4.Id, 169m, 189.99m, "admin_update", now.AddDays(-10)),
            MakeSnapshot(d4.Id, 149m, 189.99m, "admin_update", now.AddDays(-5)));

        // ── Notification Preferences ────────────────────────────────────────
        var alicePref = UserNotificationPreference.Create(alice.Id, tenantId);
        alicePref.Update(true, true, false, NotificationFrequency.Instant);
        var bobPref = UserNotificationPreference.Create(bob.Id, tenantId);
        bobPref.Update(true, true, false, NotificationFrequency.DailyDigest);
        db.NotificationPreferences.AddRange(alicePref, bobPref);

        // ── Single save — all PendingApproval, no domain events fire ────────
        await db.SaveChangesAsync();

        // ── Flip statuses via parameterized SQL to avoid domain events ──────
        var active = (int)DealStatus.Active;
        foreach (var id in new[] { d1.Id, d2.Id, d3.Id, d4.Id, d5.Id, d6.Id, d7.Id })
            await db.Database.ExecuteSqlAsync($"UPDATE deals SET status = {active} WHERE id = {id}");

        var exp = (int)DealStatus.Expired;
        await db.Database.ExecuteSqlAsync($"UPDATE deals SET status = {exp}, expires_at_utc = NOW() WHERE id = {expired.Id}");

        var rej = (int)DealStatus.Rejected;
        await db.Database.ExecuteSqlAsync($"UPDATE deals SET status = {rej} WHERE id = {rejected.Id}");

        logger.LogInformation("Seed complete: 1 tenant, 6 categories, 3 users, 11 deals (7 active, 2 pending, 1 expired, 1 rejected)");
    }

    private static Deal MakeDeal(
        Guid tenantId, Guid categoryId, Guid userId,
        string title, string slug, string description,
        decimal price, decimal? originalPrice,
        string originalUrl, string affiliateUrl,
        string? imageUrl, string retailerName) =>
        Deal.Create(tenantId, categoryId, userId, title, slug, description,
            new DealPrice(price, originalPrice, "USD"),
            originalUrl, affiliateUrl, imageUrl, retailerName,
            DealStatus.PendingApproval).Value;

    private static PriceSnapshot MakeSnapshot(Guid dealId, decimal price, decimal? original, string source, DateTime at)
    {
        var s = PriceSnapshot.Record(dealId, price, original, "USD", source);
        typeof(PriceSnapshot).GetProperty(nameof(PriceSnapshot.RecordedAtUtc))!
            .GetSetMethod(true)!.Invoke(s, [at]);
        return s;
    }
}
