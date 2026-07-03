using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.ToTable("price_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DealId).IsRequired();
        builder.Property(s => s.Price).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(s => s.OriginalPrice).HasColumnType("numeric(18,2)");
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.Source).HasMaxLength(50).IsRequired();
        builder.Property(s => s.RecordedAtUtc).IsRequired();

        builder.HasIndex(s => new { s.DealId, s.RecordedAtUtc });
    }
}
