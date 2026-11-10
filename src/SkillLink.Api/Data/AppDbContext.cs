using Microsoft.EntityFrameworkCore;

namespace SkillLink.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Connection> Connections => Set<Connection>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("citext");
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}