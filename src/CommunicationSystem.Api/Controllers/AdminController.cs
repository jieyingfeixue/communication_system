using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(AdminService adminService, MessageService messageService, MessageNotifyService notifyService, OnlineUserService onlineUsers) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserQuery query) =>
        Ok(await adminService.GetUsersAsync(query, onlineUsers));

    [HttpPost("users/{userId:int}/approve")]
    public async Task<IActionResult> Approve(int userId, [FromQuery] bool approve = true)
    {
        var result = await adminService.ApproveUserAsync(userId, approve);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("users/{userId:int}/disable")]
    public async Task<IActionResult> Disable(int userId)
    {
        var result = await adminService.SetUserStatusAsync(userId, UserStatus.Disabled);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("users/{userId:int}/enable")]
    public async Task<IActionResult> Enable(int userId)
    {
        var result = await adminService.SetUserStatusAsync(userId, UserStatus.Active);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] AdminMessageQuery query) =>
        Ok(await adminService.QueryMessagesAsync(query));

    [HttpDelete("messages/{messageId:long}")]
    public async Task<IActionResult> DeleteMessage(long messageId)
    {
        var result = await messageService.DeleteMessageAsync(0, messageId, isAdmin: true);
        if (result.Success && result.Deleted is not null)
            await notifyService.NotifyMessageDeletedAsync(result.Deleted);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }
}
