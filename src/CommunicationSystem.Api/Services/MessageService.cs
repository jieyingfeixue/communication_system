using CommunicationSystem.Api.Data;
using CommunicationSystem.Shared;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Entities;
using CommunicationSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunicationSystem.Api.Services;

public class MessageService(AppDbContext db, FriendService friendService, GroupService groupService)
{
    public async Task<(bool Success, string Message, MessageDto? Data)> SendMessageAsync(int senderId, SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return (false, "消息内容不能为空", null);

        if (request.ReceiverId.HasValue == request.GroupId.HasValue)
            return (false, "请指定私聊对象或群组", null);

        if (request.ReceiverId.HasValue)
        {
            var friends = await friendService.GetFriendsAsync(senderId);
            if (friends.All(f => f.UserId != request.ReceiverId.Value))
                return (false, "只能给好友发送私聊消息", null);
        }

        if (request.GroupId.HasValue)
        {
            if (!await groupService.IsMemberAsync(senderId, request.GroupId.Value))
                return (false, "你不是该群成员", null);
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Content = request.Content,
            MessageType = request.MessageType
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var sender = await db.Users.FindAsync(senderId);
        return (true, "发送成功", ToDto(message, sender!.Nickname));
    }

    public async Task<List<MessageDto>> GetMessagesAsync(int userId, MessageQueryRequest query)
    {
        IQueryable<Message> q = db.Messages.Include(m => m.Sender).AsQueryable();

        if (query.FriendId.HasValue)
        {
            var fid = query.FriendId.Value;
            q = q.Where(m =>
                (m.SenderId == userId && m.ReceiverId == fid) ||
                (m.SenderId == fid && m.ReceiverId == userId));
        }
        else if (query.GroupId.HasValue)
        {
            if (!await groupService.IsMemberAsync(userId, query.GroupId.Value))
                return [];
            q = q.Where(m => m.GroupId == query.GroupId.Value);
        }
        else
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(m => m.Content.Contains(query.Keyword));
        if (query.From.HasValue)
            q = q.Where(m => m.CreateTime >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(m => m.CreateTime <= query.To.Value);

        return await q.OrderByDescending(m => m.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => ToDto(m, m.Sender.Nickname))
            .ToListAsync();
    }

    public async Task MarkPrivateMessagesReadAsync(int userId, int friendId)
    {
        var messages = await db.Messages
            .Where(m => m.SenderId == friendId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        foreach (var m in messages) m.IsRead = true;
        await db.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message, Message? Deleted)> RecallMessageAsync(int userId, long messageId)
    {
        var message = await db.Messages.FindAsync(messageId);
        if (message is null) return (false, "消息不存在", null);

        if (message.GroupId.HasValue)
        {
            if (!await groupService.IsMemberAsync(userId, message.GroupId.Value))
                return (false, "无权操作该消息", null);

            var isOwner = await groupService.IsOwnerAsync(userId, message.GroupId.Value);
            if (!isOwner)
            {
                if (message.SenderId != userId)
                    return (false, "只能撤回自己发送的消息", null);
                if (!DateTimeHelper.WithinMinutes(message.CreateTime, 2))
                    return (false, "只能撤回2分钟内的消息", null);
            }
        }
        else
        {
            if (message.SenderId != userId)
                return (false, "只能撤回自己发送的消息", null);
            if (!DateTimeHelper.WithinMinutes(message.CreateTime, 2))
                return (false, "只能撤回2分钟内的消息", null);
        }

        db.Messages.Remove(message);
        await db.SaveChangesAsync();
        return (true, "消息已撤回", message);
    }

    public async Task<(bool Success, string Message, Message? Deleted)> DeleteMessageAsync(int userId, long messageId, bool isAdmin = false)
    {
        var message = await db.Messages.FindAsync(messageId);
        if (message is null) return (false, "消息不存在", null);
        if (!isAdmin && message.SenderId != userId)
            return (false, "只能删除自己发送的消息", null);

        db.Messages.Remove(message);
        await db.SaveChangesAsync();
        return (true, "删除成功", message);
    }

    public async Task<(bool Success, string Message)> ClearChatHistoryAsync(int userId, int? friendId, int? groupId)
    {
        IQueryable<Message> q = db.Messages;
        if (friendId.HasValue)
            q = q.Where(m =>
                (m.SenderId == userId && m.ReceiverId == friendId) ||
                (m.SenderId == friendId && m.ReceiverId == userId));
        else if (groupId.HasValue)
            q = q.Where(m => m.GroupId == groupId);
        else
            return (false, "请指定会话");

        db.Messages.RemoveRange(await q.ToListAsync());
        await db.SaveChangesAsync();
        return (true, "聊天记录已清空");
    }

    public static MessageDto ToDto(Message m, string senderNickname) => new(
        m.Id, m.SenderId, senderNickname, m.ReceiverId, m.GroupId,
        m.Content, m.MessageType, m.CreateTime, m.IsRead);
}
