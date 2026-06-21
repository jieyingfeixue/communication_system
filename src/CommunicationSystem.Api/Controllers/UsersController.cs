using System.Security.Claims;
using CommunicationSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(FriendService friendService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Ok(await friendService.GetRecommendedUsersAsync(UserId));
        return Ok(await friendService.SearchUsersAsync(UserId, keyword));
    }
}
