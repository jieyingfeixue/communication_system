using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.DTOs;

public record FriendRequestDto(int FriendshipId, int FromUserId, string FromUsername, string FromNickname, DateTime CreateTime);

public record FriendDto(
    int UserId,
    string Username,
    string Nickname,
    string? Avatar,
    bool IsOnline,
    int UnreadCount = 0,
    DateTime? LastMessageTime = null);

public record FriendshipActionRequest(int FriendshipId);
public record AddFriendRequest(int TargetUserId);
