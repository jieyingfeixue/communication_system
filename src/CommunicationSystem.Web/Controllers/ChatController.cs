using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Web.Filters;
using CommunicationSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Web.Controllers;

[RequireLogin]
public class ChatController(ApiClient api) : Controller
{
    private static object ActionResult((bool Success, string Message) result) =>
        new { success = result.Success, message = result.Message };

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Friends() =>
        Json(await api.GetAsync<List<FriendDto>>("/api/friends") ?? []);

    [HttpGet]
    public async Task<IActionResult> Groups() =>
        Json(await api.GetAsync<List<GroupDto>>("/api/groups") ?? []);

    [HttpGet]
    public async Task<IActionResult> Requests() =>
        Json(await api.GetAsync<List<FriendRequestDto>>("/api/friends/requests") ?? []);

    [HttpGet]
    public async Task<IActionResult> Messages(int? friendId, int? groupId, string? keyword)
    {
        var url = "/api/messages?Page=1&PageSize=100";
        if (friendId.HasValue) url += $"&FriendId={friendId}";
        if (groupId.HasValue) url += $"&GroupId={groupId}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&Keyword={Uri.EscapeDataString(keyword)}";
        return Json(await api.GetAsync<List<MessageDto>>(url) ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> SearchUsers(string? keyword)
    {
        var url = string.IsNullOrWhiteSpace(keyword)
            ? "/api/users/search"
            : $"/api/users/search?keyword={Uri.EscapeDataString(keyword.Trim())}";
        return Json(await api.GetAsync<List<UserSummaryDto>>(url) ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> AddFriend([FromBody] AddFriendRequest request) =>
        Json(ActionResult(await api.PostAsync("/api/friends/request", request)));

    [HttpPost]
    public async Task<IActionResult> AcceptFriend([FromBody] FriendshipActionRequest request) =>
        Json(ActionResult(await api.PostAsync("/api/friends/accept", request)));

    [HttpPost]
    public async Task<IActionResult> RejectFriend([FromBody] FriendshipActionRequest request) =>
        Json(ActionResult(await api.PostAsync("/api/friends/reject", request)));

    [HttpDelete]
    public async Task<IActionResult> DeleteFriend(int friendId) =>
        Json(ActionResult(await api.DeleteAsync($"/api/friends/{friendId}")));

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var result = await api.PostAsync("/api/groups", request);
        return Json(ActionResult(result));
    }

    [HttpGet]
    public async Task<IActionResult> GroupMembers(int groupId) =>
        Json(await api.GetAsync<List<GroupMemberDto>>($"/api/groups/{groupId}/members") ?? []);

    [HttpPost]
    public async Task<IActionResult> InviteToGroup([FromBody] InviteToGroupRequest request) =>
        Json(ActionResult(await api.PostAsync("/api/groups/invite", request)));

    [HttpPost]
    public async Task<IActionResult> LeaveGroup([FromBody] RemoveFromGroupRequest request) =>
        Json(ActionResult(await api.PostAsync("/api/groups/leave", request)));

    [HttpPost]
    public async Task<IActionResult> RecallMessage(long id) =>
        Json(ActionResult(await api.PostAsync($"/api/messages/{id}/recall")));

    [HttpDelete]
    public async Task<IActionResult> DissolveGroup(int groupId) =>
        Json(ActionResult(await api.DeleteAsync($"/api/groups/{groupId}")));

    [HttpDelete]
    public async Task<IActionResult> DeleteMessage(long id) =>
        Json(ActionResult(await api.DeleteAsync($"/api/messages/{id}")));

    [HttpDelete]
    public async Task<IActionResult> ClearHistory(int? friendId, int? groupId)
    {
        var url = "/api/messages/clear?";
        if (friendId.HasValue) url += $"friendId={friendId}";
        if (groupId.HasValue) url += $"groupId={groupId}";
        return Json(ActionResult(await api.DeleteAsync(url)));
    }
}
