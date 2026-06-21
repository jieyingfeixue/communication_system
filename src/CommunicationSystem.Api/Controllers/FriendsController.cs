using System.Security.Claims;
using CommunicationSystem.Api.Hubs;
using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(FriendService friendService, IHubContext<ChatHub> hub) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetFriends() =>
        Ok(await friendService.GetFriendsAsync(UserId));

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests() =>
        Ok(await friendService.GetPendingRequestsAsync(UserId));

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] AddFriendRequest request)
    {
        var result = await friendService.SendFriendRequestAsync(UserId, request.TargetUserId);
        if (result.Success)
        {
            var requests = await friendService.GetPendingRequestsAsync(request.TargetUserId);
            var latest = requests.FirstOrDefault(r => r.FromUserId == UserId);
            if (latest is not null)
                await hub.Clients.User(request.TargetUserId.ToString())
                    .SendAsync("FriendRequestReceived", latest);
        }
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] FriendshipActionRequest request)
    {
        var friendship = await Respond(request.FriendshipId, true);
        return friendship;
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] FriendshipActionRequest request) =>
        await Respond(request.FriendshipId, false);

    private async Task<IActionResult> Respond(int friendshipId, bool accept)
    {
        var result = await friendService.RespondToRequestAsync(UserId, friendshipId, accept);
        if (result.Success && accept)
        {
            await hub.Clients.User(UserId.ToString()).SendAsync("FriendListChanged");
            if (result.RequesterId.HasValue)
                await hub.Clients.User(result.RequesterId.Value.ToString()).SendAsync("FriendListChanged");
        }

        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpDelete("{friendId:int}")]
    public async Task<IActionResult> Delete(int friendId)
    {
        var result = await friendService.DeleteFriendAsync(UserId, friendId);
        if (result.Success)
        {
            await hub.Clients.User(UserId.ToString()).SendAsync("FriendListChanged");
            await hub.Clients.User(friendId.ToString()).SendAsync("FriendListChanged");
        }
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }
}
