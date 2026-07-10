using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Endpoint).HasMaxLength(2048).IsRequired();
        builder.Property(p => p.P256dh).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Auth).HasMaxLength(128).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.HasIndex(p => new { p.UserId, p.TenantId });
        builder.HasIndex(p => p.Endpoint).IsUnique();
    }
}
