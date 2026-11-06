namespace SkillLink.Api.Features.Users;

public sealed record CreateUserRequest(string Email, string DisplayName, string? Headline);

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string? Headline);