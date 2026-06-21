using CommunicationSystem.Api.Hubs;
using CommunicationSystem.Shared.Entities;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationSystem.Api.Services;

public class MessageNotifyService(IHubContext<ChatHub> hub, GroupService groupService)
{
    public async Task NotifyMessageDeletedAsync(Message message)
    {
        var userIds = await GetAffectedUserIdsAsync(message);
        foreach (var uid in userIds)
        {
            await hub.Clients.User(uid.ToString()).SendAsync("MessageDeleted", message.Id);
            if (!message.GroupId.HasValue)
                await hub.Clients.User(uid.ToString()).SendAsync("FriendListChanged");
        }
    }

    public async Task NotifyChatClearedAsync(int userId, int? friendId, int? groupId)
    {
        var userIds = new HashSet<int> { userId };
        if (friendId.HasValue)
            userIds.Add(friendId.Value);
        else if (groupId.HasValue)
            foreach (var id in await groupService.GetGroupMemberIdsAsync(groupId.Value))
                userIds.Add(id);

        foreach (var uid in userIds)
            await hub.Clients.User(uid.ToString()).SendAsync("ChatHistoryCleared", friendId, groupId);
    }

    public async Task NotifyGroupDissolvedAsync(int groupId, IEnumerable<int> memberIds)
    {
        foreach (var uid in memberIds)
            await hub.Clients.User(uid.ToString()).SendAsync("GroupDissolved", groupId);
    }

    public async Task NotifyGroupListChangedAsync(IEnumerable<int> userIds)
    {
        foreach (var uid in userIds.Distinct())
            await hub.Clients.User(uid.ToString()).SendAsync("GroupListChanged");
    }

    private async Task<HashSet<int>> GetAffectedUserIdsAsync(Message message)
    {
        var userIds = new HashSet<int> { message.SenderId };
        if (message.ReceiverId.HasValue)
            userIds.Add(message.ReceiverId.Value);
        else if (message.GroupId.HasValue)
            foreach (var id in await groupService.GetGroupMemberIdsAsync(message.GroupId.Value))
                userIds.Add(id);
        return userIds;
    }
}
