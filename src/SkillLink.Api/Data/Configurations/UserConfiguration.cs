using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SkillLink.Api.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> u)
    {
        u.Property(x => x.Email).HasColumnType("citext");
        u.HasIndex(x => x.Email).IsUnique();             // prevents the duplicate-email race condition
        u.Property(x => x.DisplayName).HasMaxLength(100);
        u.Property(x => x.Headline).HasMaxLength(200);
    }
}