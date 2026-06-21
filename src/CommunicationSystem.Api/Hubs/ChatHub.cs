using System.Security.Claims;
using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationSystem.Api.Hubs;

[Authorize]
public class ChatHub(
    OnlineUserService onlineUsers,
    MessageService messageService,
    GroupService groupService) : Hub
{
    private int UserId => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        onlineUsers.UserConnected(UserId, Context.ConnectionId);
        await Clients.Others.SendAsync("UserOnline", UserId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        onlineUsers.UserDisconnected(UserId, Context.ConnectionId);
        if (!onlineUsers.IsOnline(UserId))
            await Clients.Others.SendAsync("UserOffline", UserId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var result = await messageService.SendMessageAsync(UserId, request);
        if (!result.Success || result.Data is null)
        {
            await Clients.Caller.SendAsync("Error", result.Message);
            return;
        }

        var dto = result.Data;
        if (request.ReceiverId.HasValue)
        {
            await Clients.User(request.ReceiverId.Value.ToString()).SendAsync("ReceiveMessage", dto);
            await Clients.Caller.SendAsync("ReceiveMessage", dto);
            await Clients.User(request.ReceiverId.Value.ToString()).SendAsync("FriendListChanged");
            await Clients.Caller.SendAsync("FriendListChanged");
        }
        else if (request.GroupId.HasValue)
        {
            var members = await groupService.GetGroupMemberIdsAsync(request.GroupId.Value);
            foreach (var memberId in members)
                await Clients.User(memberId.ToString()).SendAsync("ReceiveMessage", dto);
        }
    }

    public async Task MarkAsRead(int friendId)
    {
        await messageService.MarkPrivateMessagesReadAsync(UserId, friendId);
        await Clients.User(friendId.ToString()).SendAsync("MessagesRead", UserId);
        await Clients.Caller.SendAsync("FriendListChanged");
    }

    public async Task NotifyTyping(int? receiverId, int? groupId)
    {
        var notification = new TypingNotification(UserId,
            Context.User!.FindFirstValue("nickname") ?? "", receiverId, groupId);

        if (receiverId.HasValue)
            await Clients.User(receiverId.Value.ToString()).SendAsync("UserTyping", notification);
        else if (groupId.HasValue)
        {
            var members = await groupService.GetGroupMemberIdsAsync(groupId.Value);
            foreach (var id in members.Where(id => id != UserId))
                await Clients.User(id.ToString()).SendAsync("UserTyping", notification);
        }
    }

    public async Task NotifyFriendRequest(int targetUserId, FriendRequestDto request)
    {
        await Clients.User(targetUserId.ToString()).SendAsync("FriendRequestReceived", request);
    }

    public async Task NotifyFriendAccepted(int targetUserId)
    {
        await Clients.User(targetUserId.ToString()).SendAsync("FriendAccepted", UserId);
    }
}
