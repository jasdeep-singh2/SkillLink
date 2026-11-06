namespace SkillLink.Api.Data;

public sealed class User
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? Headline { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<UserSkill> Skills { get; } = [];
}

public sealed class Skill
{
    public int Id { get; init; }
    public required string Name { get; set; }
}

public sealed class UserSkill
{
    public Guid UserId { get; init; }
    public int SkillId { get; init; }
    public short Level { get; set; }
    public User User { get; init; } = null!;
    public Skill Skill { get; init; } = null!;
}