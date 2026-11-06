using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SkillLink.Api.Data.Configurations;

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> s)
    {
        s.Property(x => x.Name).HasColumnType("citext");
        s.HasIndex(x => x.Name).IsUnique();
    }
}