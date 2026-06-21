using CommunicationSystem.Api.Data;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunicationSystem.Api.Services;

public class AdminService(AppDbContext db)
{
    public async Task<List<UserSummaryDto>> GetUsersAsync(AdminUserQuery query, OnlineUserService onlineUsers)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Username))
            q = q.Where(u => u.Username.Contains(query.Username) || u.Nickname.Contains(query.Username));
        if (query.Status.HasValue)
            q = q.Where(u => u.Status == query.Status.Value);

        var users = await q.OrderByDescending(u => u.Id).ToListAsync();
        return users.Select(u => new UserSummaryDto(
            u.Id, u.Username, u.Nickname, u.Avatar, u.Status, u.Role,
            onlineUsers.IsOnline(u.Id))).ToList();
    }

    public async Task<(bool Success, string Message)> ApproveUserAsync(int userId, bool approve)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return (false, "用户不存在");
        if (user.Role == UserRole.Admin) return (false, "不能操作管理员账号");

        if (approve)
        {
            user.Status = UserStatus.Active;
            await db.SaveChangesAsync();
            return (true, "已通过注册申请");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return (true, "已拒绝并删除注册申请");
    }

    public async Task<(bool Success, string Message)> SetUserStatusAsync(int userId, UserStatus status)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return (false, "用户不存在");
        if (user.Role == UserRole.Admin) return (false, "不能操作管理员账号");

        user.Status = status;
        await db.SaveChangesAsync();
        return (true, status == UserStatus.Disabled ? "已禁用账号" : "已解禁账号");
    }

    public async Task<List<MessageDto>> QueryMessagesAsync(AdminMessageQuery query)
    {
        var q = db.Messages.Include(m => m.Sender).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(m => m.Content.Contains(query.Keyword));
        if (query.SenderId.HasValue)
            q = q.Where(m => m.SenderId == query.SenderId);
        if (query.From.HasValue)
            q = q.Where(m => m.CreateTime >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(m => m.CreateTime <= query.To.Value);

        return await q.OrderByDescending(m => m.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => MessageService.ToDto(m, m.Sender.Nickname))
            .ToListAsync();
    }
}
