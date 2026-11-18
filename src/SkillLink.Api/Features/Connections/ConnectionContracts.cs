namespace SkillLink.Api.Features.Connections;

public sealed record SendConnectionRequest(Guid AddresseeId);   // the requester is whoever is logged in

public sealed record ConnectionResponse(Guid UserId, string DisplayName);

public sealed record SuggestionResponse(Guid Id, string DisplayName, int MutualCount);