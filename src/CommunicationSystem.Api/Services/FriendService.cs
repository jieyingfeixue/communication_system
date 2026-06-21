using CommunicationSystem.Api.Data;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Entities;
using CommunicationSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunicationSystem.Api.Services;

public class FriendService(AppDbContext db, OnlineUserService onlineUsers)
{
    public async Task<List<UserSummaryDto>> SearchUsersAsync(int currentUserId, string keyword)
    {
        keyword = keyword.Trim();
        var users = await db.Users
            .Where(u => u.Id != currentUserId && u.Role == UserRole.User &&
                        (u.Username.Contains(keyword) || u.Nickname.Contains(keyword)))
            .Take(20)
            .ToListAsync();

        return users.Select(u => new UserSummaryDto(
            u.Id, u.Username, u.Nickname, u.Avatar, u.Status, u.Role,
            onlineUsers.IsOnline(u.Id))).ToList();
    }

    public async Task<List<UserSummaryDto>> GetRecommendedUsersAsync(int currentUserId, int count = 3)
    {
        var relatedIds = await db.Friendships
            .Where(f => f.UserId1 == currentUserId || f.UserId2 == currentUserId)
            .Select(f => f.UserId1 == currentUserId ? f.UserId2 : f.UserId1)
            .ToListAsync();

        var excluded = relatedIds.Append(currentUserId).ToHashSet();

        var candidates = await db.Users
            .Where(u => u.Role == UserRole.User && u.Status == UserStatus.Active && !excluded.Contains(u.Id))
            .ToListAsync();

        return candidates
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .Select(u => new UserSummaryDto(
                u.Id, u.Username, u.Nickname, u.Avatar, u.Status, u.Role,
                onlineUsers.IsOnline(u.Id)))
            .ToList();
    }

    public async Task<(bool Success, string Message)> SendFriendRequestAsync(int fromUserId, int targetUserId)
    {
        if (fromUserId == targetUserId)
            return (false, "不能添加自己为好友");

        var target = await db.Users.FindAsync(targetUserId);
        if (target is null || target.Status != UserStatus.Active)
            return (false, "目标用户不存在或不可用");

        var existing = await db.Friendships.FirstOrDefaultAsync(f =>
            (f.UserId1 == fromUserId && f.UserId2 == targetUserId) ||
            (f.UserId1 == targetUserId && f.UserId2 == fromUserId));

        if (existing is not null)
        {
            return existing.Status switch
            {
                FriendshipStatus.Accepted => (false, "你们已经是好友"),
                FriendshipStatus.Pending => (false, "好友申请已存在"),
                _ => (false, "好友申请处理中")
            };
        }

        db.Friendships.Add(new Friendship
        {
            UserId1 = fromUserId,
            UserId2 = targetUserId,
            Status = FriendshipStatus.Pending
        });
        await db.SaveChangesAsync();
        return (true, "好友申请已发送");
    }

    public async Task<List<FriendRequestDto>> GetPendingRequestsAsync(int userId)
    {
        return await db.Friendships
            .Include(f => f.User1)
            .Where(f => f.Status == FriendshipStatus.Pending && f.UserId2 == userId)
            .Select(f => new FriendRequestDto(
                f.Id,
                f.UserId1,
                f.User1.Username,
                f.User1.Nickname,
                f.CreateTime))
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, int? RequesterId)> RespondToRequestAsync(int userId, int friendshipId, bool accept)
    {
        var friendship = await db.Friendships.FindAsync(friendshipId);
        if (friendship is null || friendship.Status != FriendshipStatus.Pending)
            return (false, "申请不存在或已处理", null);

        if (friendship.UserId2 != userId)
            return (false, "无权处理此申请", null);

        var requesterId = friendship.UserId1;
        if (!accept)
        {
            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();
            return (true, "已拒绝好友申请", requesterId);
        }

        friendship.Status = FriendshipStatus.Accepted;
        await db.SaveChangesAsync();
        return (true, "已同意好友申请", requesterId);
    }

    public async Task<List<FriendDto>> GetFriendsAsync(int userId)
    {
        var friendships = await db.Friendships
            .Include(f => f.User1).Include(f => f.User2)
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.UserId1 == userId || f.UserId2 == userId))
            .ToListAsync();

        var friendIds = friendships
            .Select(f => f.UserId1 == userId ? f.UserId2 : f.UserId1)
            .ToHashSet();

        var privateMessages = await db.Messages
            .Where(m => m.GroupId == null &&
                        ((m.SenderId == userId && m.ReceiverId != null) || m.ReceiverId == userId))
            .Select(m => new { m.SenderId, m.ReceiverId, m.CreateTime, m.IsRead })
            .ToListAsync();

        var stats = new Dictionary<int, (int Unread, DateTime? Last)>();
        foreach (var m in privateMessages)
        {
            var friendId = m.SenderId == userId ? m.ReceiverId!.Value : m.SenderId;
            if (!friendIds.Contains(friendId)) continue;

            stats.TryGetValue(friendId, out var s);
            var last = s.Last;
            if (last is null || m.CreateTime > last)
                last = m.CreateTime;
            var unread = s.Unread;
            if (m.ReceiverId == userId && !m.IsRead)
                unread++;
            stats[friendId] = (unread, last);
        }

        return friendships.Select(f =>
        {
            var friend = f.UserId1 == userId ? f.User2 : f.User1;
            stats.TryGetValue(friend.Id, out var st);
            return new FriendDto(friend.Id, friend.Username, friend.Nickname, friend.Avatar,
                onlineUsers.IsOnline(friend.Id), st.Unread, st.Last);
        })
        .OrderByDescending(x => x.LastMessageTime ?? DateTime.MinValue)
        .ThenBy(x => x.Nickname)
        .ToList();
    }

    public async Task<(bool Success, string Message)> DeleteFriendAsync(int userId, int friendId)
    {
        var friendship = await db.Friendships.FirstOrDefaultAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.UserId1 == userId && f.UserId2 == friendId) ||
             (f.UserId1 == friendId && f.UserId2 == userId)));
        if (friendship is null)
            return (false, "好友关系不存在");

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync();
        return (true, "已删除好友");
    }
}
