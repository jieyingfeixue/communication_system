using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Shared.DTOs;

public record RegisterRequest(string Username, string Password, string Nickname);
public record LoginRequest(string Username, string Password);

public record AuthResponse(
    string Token,
    int UserId,
    string Username,
    string Nickname,
    string? Avatar,
    UserRole Role,
    UserStatus Status);

public record UserSummaryDto(
    int Id,
    string Username,
    string Nickname,
    string? Avatar,
    UserStatus Status,
    UserRole Role,
    bool IsOnline);
