using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class WatchlistEntryConfiguration : IEntityTypeConfiguration<WatchlistEntry>
{
    public void Configure(EntityTypeBuilder<WatchlistEntry> builder)
    {
        builder.ToTable("watchlist_entries");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.UserId).IsRequired();
        builder.Property(w => w.TenantId).IsRequired();
        builder.Property(w => w.ProductKeyword).HasMaxLength(100);
        builder.Property(w => w.TargetPrice).HasColumnType("numeric(18,2)");
        builder.HasIndex(w => new { w.UserId, w.TenantId });
        builder.HasIndex(w => new { w.TenantId, w.CategoryId, w.IsActive });
    }
}
