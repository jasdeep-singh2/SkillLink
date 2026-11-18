namespace SkillLink.Api.Features.Users;

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string? Headline);