namespace CommunicationSystem.Shared.DTOs;

public record CreateGroupRequest(string GroupName, List<int> MemberUserIds);
public record GroupDto(int Id, string GroupName, int OwnerId, string OwnerNickname, DateTime CreateTime, int MemberCount);
public record GroupMemberDto(int UserId, string Username, string Nickname, string? Avatar, DateTime JoinTime);
public record InviteToGroupRequest(int GroupId, List<int> UserIds);
public record RemoveFromGroupRequest(int GroupId, int UserId);
