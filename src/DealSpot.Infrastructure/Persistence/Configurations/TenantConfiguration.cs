using DealSpot.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Domain).HasMaxLength(200).IsRequired();
        builder.OwnsOne(t => t.Branding, branding => { branding.ToJson(); });
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.Domain).IsUnique();
    }
}
