using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SkillLink.Api.Data.Configurations;

internal sealed class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> us)
    {
        us.HasKey(x => new { x.UserId, x.SkillId });
        us.HasIndex(x => new { x.SkillId, x.Level }).IsDescending(false, true);  // "who knows X?"
        us.ToTable(t => t.HasCheckConstraint("ck_user_skills_level", "level BETWEEN 1 AND 5"));
        us.HasOne(x => x.User).WithMany(u => u.Skills).HasForeignKey(x => x.UserId);
    }
}