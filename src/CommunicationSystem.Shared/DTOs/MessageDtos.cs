using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.DTOs;

public record SendMessageRequest(
    int? ReceiverId,
    int? GroupId,
    string Content,
    MessageType MessageType = MessageType.Text);

public record MessageDto(
    long Id,
    int SenderId,
    string SenderNickname,
    int? ReceiverId,
    int? GroupId,
    string Content,
    MessageType MessageType,
    DateTime CreateTime,
    bool IsRead);

public record MessageQueryRequest(
    int? FriendId,
    int? GroupId,
    string? Keyword,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);

public record MarkReadRequest(int FriendId);
