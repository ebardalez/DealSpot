using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Frequency).IsRequired();
        builder.HasIndex(p => new { p.UserId, p.TenantId }).IsUnique();
    }
}

internal sealed class PendingAlertConfiguration : IEntityTypeConfiguration<PendingAlert>
{
    public void Configure(EntityTypeBuilder<PendingAlert> builder)
    {
        builder.ToTable("pending_alerts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.DealId).IsRequired();
        builder.Property(a => a.AlertType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.MatchedKeyword).HasMaxLength(100);
        builder.HasIndex(a => a.IsSent);
        builder.HasIndex(a => new { a.UserId, a.IsSent });
    }
}
