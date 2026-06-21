using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.DTOs;

public record AdminUserQuery(string? Username, UserStatus? Status);
public record AdminMessageQuery(string? Keyword, int? SenderId, DateTime? From, DateTime? To, int Page = 1, int PageSize = 50);
public record AdminActionResponse(bool Success, string Message);
