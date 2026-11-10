namespace SkillLink.Api.Features.Connections;

public sealed record SendConnectionRequest(Guid RequesterId, Guid AddresseeId);

public sealed record ConnectionResponse(Guid UserId, string DisplayName);

public sealed record SuggestionResponse(Guid Id, string DisplayName, int MutualCount);