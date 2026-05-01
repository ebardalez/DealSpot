using DealSpot.Domain.AffiliateLinks;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options), IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<AffiliateNetwork> AffiliateNetworks => Set<AffiliateNetwork>();
    public DbSet<User> AppUsers => Set<User>();
    public DbSet<WatchlistEntry> WatchlistEntries => Set<WatchlistEntry>();
    public DbSet<UserNotificationPreference> NotificationPreferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await base.SaveChangesAsync(cancellationToken);
}
