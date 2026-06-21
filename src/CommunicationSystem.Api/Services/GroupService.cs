using CommunicationSystem.Api.Data;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunicationSystem.Api.Services;

public class GroupService(AppDbContext db, FriendService friendService)
{
    public async Task<(bool Success, string Message, GroupDto? Data)> CreateGroupAsync(int ownerId, CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupName))
            return (false, "群名称不能为空", null);

        var friendIds = (await friendService.GetFriendsAsync(ownerId)).Select(f => f.UserId).ToHashSet();
        var members = new HashSet<int>(request.MemberUserIds ?? []) { ownerId };
        foreach (var uid in members)
        {
            if (uid != ownerId && !friendIds.Contains(uid))
                return (false, "只能邀请好友加入群聊", null);
        }

        var group = new ChatGroup
        {
            GroupName = request.GroupName.Trim(),
            OwnerId = ownerId
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        foreach (var uid in members)
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = uid });
        await db.SaveChangesAsync();

        var owner = await db.Users.FindAsync(ownerId);
        return (true, "群聊创建成功", new GroupDto(group.Id, group.GroupName, ownerId,
            owner?.Nickname ?? "", group.CreateTime, members.Count));
    }

    public async Task<List<GroupDto>> GetMyGroupsAsync(int userId)
    {
        return await db.GroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Group).ThenInclude(g => g.Owner)
            .Include(m => m.Group).ThenInclude(g => g.Members)
            .Select(m => new GroupDto(
                m.Group.Id,
                m.Group.GroupName,
                m.Group.OwnerId,
                m.Group.Owner.Nickname,
                m.Group.CreateTime,
                m.Group.Members.Count))
            .ToListAsync();
    }

    public async Task<List<GroupMemberDto>> GetGroupMembersAsync(int userId, int groupId)
    {
        if (!await IsMemberAsync(userId, groupId))
            return [];

        return await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .Select(m => new GroupMemberDto(m.UserId, m.User.Username, m.User.Nickname, m.User.Avatar, m.JoinTime))
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> InviteMembersAsync(int userId, InviteToGroupRequest request)
    {
        var group = await db.Groups.FindAsync(request.GroupId);
        if (group is null) return (false, "群组不存在");
        if (!await IsMemberAsync(userId, request.GroupId))
            return (false, "只有群成员可以邀请好友");

        var friendIds = (await friendService.GetFriendsAsync(userId)).Select(f => f.UserId).ToHashSet();
        foreach (var uid in request.UserIds.Distinct())
        {
            if (!friendIds.Contains(uid))
                return (false, "只能邀请好友加入群聊");
            if (!await db.GroupMembers.AnyAsync(m => m.GroupId == request.GroupId && m.UserId == uid))
                db.GroupMembers.Add(new GroupMember { GroupId = request.GroupId, UserId = uid });
        }
        await db.SaveChangesAsync();
        return (true, "邀请成功");
    }

    public async Task<(bool Success, string Message)> RemoveMemberAsync(int userId, RemoveFromGroupRequest request)
    {
        var group = await db.Groups.FindAsync(request.GroupId);
        if (group is null) return (false, "群组不存在");
        if (group.OwnerId != userId) return (false, "只有群主可以移除成员");
        if (request.UserId == userId) return (false, "不能移除自己");

        var member = await db.GroupMembers.FirstOrDefaultAsync(m =>
            m.GroupId == request.GroupId && m.UserId == request.UserId);
        if (member is null) return (false, "成员不在群中");

        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync();
        return (true, "已移除成员");
    }

    public async Task<(bool Success, string Message, List<int>? MemberIds)> LeaveGroupAsync(int userId, int groupId)
    {
        var group = await db.Groups.FindAsync(groupId);
        if (group is null) return (false, "群组不存在", null);
        if (group.OwnerId == userId)
            return (false, "群主请使用解散群聊，不能直接退出", null);

        var member = await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member is null) return (false, "您不在该群中", null);

        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync();
        return (true, "已退出群聊", [userId]);
    }

    public async Task<(bool Success, string Message, List<int>? MemberIds)> DissolveGroupAsync(int userId, int groupId)
    {
        var group = await db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null) return (false, "群组不存在", null);
        if (group.OwnerId != userId) return (false, "只有群主可以解散群聊", null);

        var memberIds = group.Members.Select(m => m.UserId).ToList();
        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        return (true, "群聊已解散", memberIds);
    }

    public async Task<bool> IsMemberAsync(int userId, int groupId) =>
        await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

    public async Task<bool> IsOwnerAsync(int userId, int groupId)
    {
        var group = await db.Groups.FindAsync(groupId);
        return group is not null && group.OwnerId == userId;
    }

    public async Task<List<int>> GetGroupMemberIdsAsync(int groupId) =>
        await db.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId).ToListAsync();
}
