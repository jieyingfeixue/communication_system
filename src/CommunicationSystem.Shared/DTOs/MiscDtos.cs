namespace CommunicationSystem.Shared.DTOs;

public record FileUploadResponse(string FileName, string Url, long Size);
public record TypingNotification(int UserId, string Nickname, int? ReceiverId, int? GroupId);
