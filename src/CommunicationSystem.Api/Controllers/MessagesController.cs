using System.Security.Claims;
using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController(MessageService messageService, MessageNotifyService notifyService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] MessageQueryRequest query) =>
        Ok((await messageService.GetMessagesAsync(UserId, query)).OrderBy(m => m.CreateTime).ToList());

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var result = await messageService.SendMessageAsync(UserId, request);
        return result.Success ? Ok(result.Data) : BadRequest(new { result.Message });
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest request)
    {
        await messageService.MarkPrivateMessagesReadAsync(UserId, request.FriendId);
        return Ok();
    }

    [HttpPost("{messageId:long}/recall")]
    public async Task<IActionResult> Recall(long messageId)
    {
        var result = await messageService.RecallMessageAsync(UserId, messageId);
        if (result.Success && result.Deleted is not null)
            await notifyService.NotifyMessageDeletedAsync(result.Deleted);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpDelete("{messageId:long}")]
    public async Task<IActionResult> Delete(long messageId)
    {
        var result = await messageService.DeleteMessageAsync(UserId, messageId);
        if (result.Success && result.Deleted is not null)
            await notifyService.NotifyMessageDeletedAsync(result.Deleted);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear([FromQuery] int? friendId, [FromQuery] int? groupId)
    {
        var result = await messageService.ClearChatHistoryAsync(UserId, friendId, groupId);
        if (result.Success)
            await notifyService.NotifyChatClearedAsync(UserId, friendId, groupId);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }
}
