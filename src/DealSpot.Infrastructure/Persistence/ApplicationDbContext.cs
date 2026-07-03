using DealSpot.Domain.AffiliateLinks;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DealSpot.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher? publisher = null)
    : IdentityDbContext(options), IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<AffiliateNetwork> AffiliateNetworks => Set<AffiliateNetwork>();
    public DbSet<User> AppUsers => Set<User>();
    public DbSet<WatchlistEntry> WatchlistEntries => Set<WatchlistEntry>();
    public DbSet<DealVote> DealVotes => Set<DealVote>();
    public DbSet<UserNotificationPreference> NotificationPreferences => Set<UserNotificationPreference>();
    public DbSet<PendingAlert> PendingAlerts => Set<PendingAlert>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        // ponytail: dispatch inline after save, no outbox. Add outbox when a handler must survive process crash.
        if (publisher is not null)
        {
            var events = ChangeTracker.Entries<Entity>()
                .SelectMany(e =>
                {
                    var domainEvents = e.Entity.GetDomainEvents().ToList();
                    e.Entity.ClearDomainEvents();
                    return domainEvents;
                })
                .ToList();

            foreach (var domainEvent in events)
                await publisher.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
