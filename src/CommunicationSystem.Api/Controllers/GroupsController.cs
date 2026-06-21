using System.Security.Claims;
using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(GroupService groupService, MessageNotifyService notifyService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyGroups() =>
        Ok(await groupService.GetMyGroupsAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        var result = await groupService.CreateGroupAsync(UserId, request);
        if (result.Success && result.Data is not null)
        {
            var memberIds = await groupService.GetGroupMemberIdsAsync(result.Data.Id);
            await notifyService.NotifyGroupListChangedAsync(memberIds);
        }
        return result.Success ? Ok(result.Data) : BadRequest(new { result.Message });
    }

    [HttpGet("{groupId:int}/members")]
    public async Task<IActionResult> GetMembers(int groupId) =>
        Ok(await groupService.GetGroupMembersAsync(UserId, groupId));

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteToGroupRequest request)
    {
        var result = await groupService.InviteMembersAsync(UserId, request);
        if (result.Success)
        {
            var memberIds = await groupService.GetGroupMemberIdsAsync(request.GroupId);
            await notifyService.NotifyGroupListChangedAsync(memberIds);
        }
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] RemoveFromGroupRequest request)
    {
        var result = await groupService.RemoveMemberAsync(UserId, request);
        if (result.Success)
            await notifyService.NotifyGroupListChangedAsync([request.UserId, UserId]);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromBody] RemoveFromGroupRequest request)
    {
        var result = await groupService.LeaveGroupAsync(UserId, request.GroupId);
        if (result.Success && result.MemberIds is not null)
            await notifyService.NotifyGroupListChangedAsync(result.MemberIds);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpDelete("{groupId:int}")]
    public async Task<IActionResult> Dissolve(int groupId)
    {
        var result = await groupService.DissolveGroupAsync(UserId, groupId);
        if (result.Success && result.MemberIds is not null)
            await notifyService.NotifyGroupDissolvedAsync(groupId, result.MemberIds);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }
}
