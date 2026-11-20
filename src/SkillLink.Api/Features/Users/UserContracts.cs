namespace SkillLink.Api.Features.Users;

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string? Headline);

// Public view of someone else: no email.
public sealed record UserSummary(Guid Id, string DisplayName, string? Headline);