using DealSpot.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealSpot.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("app_users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
