using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SkillLink.Api.Data.Configurations;

internal sealed class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> c)
    {
        c.HasKey(x => new { x.RequesterId, x.AddresseeId });            // also serves "requests I sent"
        c.HasIndex(x => new { x.AddresseeId, x.Status });               // "requests sent to me"
        c.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        c.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterId);
        c.HasOne<User>().WithMany().HasForeignKey(x => x.AddresseeId);
        c.ToTable(t => t.HasCheckConstraint("ck_connections_not_self", "requester_id <> addressee_id"));
    }
}