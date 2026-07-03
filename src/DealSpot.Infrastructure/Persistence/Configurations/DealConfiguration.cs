using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CategoryId).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Slug).HasMaxLength(250).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.OriginalUrl).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.AffiliateUrl).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.ImageUrl).HasMaxLength(500);
        builder.Property(d => d.RetailerName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Status).IsRequired();

        builder.OwnsOne(d => d.Price, price =>
        {
            price.Property(p => p.CurrentPrice).HasColumnName("current_price").HasColumnType("numeric(18,2)").IsRequired();
            price.Property(p => p.OriginalPrice).HasColumnName("original_price").HasColumnType("numeric(18,2)");
            price.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(d => d.DealScore, score =>
        {
            score.Property(s => s.Value).HasColumnName("deal_score_value");
            score.Property(s => s.DiscountComponent).HasColumnName("score_discount_component").HasColumnType("numeric(5,2)");
            score.Property(s => s.RetailerTrustComponent).HasColumnName("score_retailer_component").HasColumnType("numeric(5,2)");
            score.Property(s => s.VelocityComponent).HasColumnName("score_velocity_component").HasColumnType("numeric(5,2)");
            score.Property(s => s.CalculatedAtUtc).HasColumnName("score_calculated_at");
        });

        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.CategoryId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.Slug }).IsUnique();
        builder.HasIndex(d => d.CreatedAtUtc);

        // Voting denormalized counts
        builder.Property(d => d.UpVoteCount).HasDefaultValue(0);
        builder.Property(d => d.DownVoteCount).HasDefaultValue(0);
        builder.Ignore(d => d.NetScore); // computed, not persisted
    }
}
