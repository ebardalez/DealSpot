using DealSpot.Domain.Deals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class DealVoteConfiguration : IEntityTypeConfiguration<DealVote>
{
    public void Configure(EntityTypeBuilder<DealVote> builder)
    {
        builder.ToTable("deal_votes");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.DealId).IsRequired();
        builder.Property(v => v.UserId).IsRequired();
        builder.Property(v => v.Value).IsRequired();
        builder.HasIndex(v => new { v.DealId, v.UserId }).IsUnique();
        builder.HasIndex(v => v.DealId);
    }
}
