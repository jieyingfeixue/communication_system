using CommunicationSystem.Api.Services;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return result.Success ? Ok(new { result.Message }) : BadRequest(new { result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result.Success ? Ok(result.Data) : BadRequest(new { result.Message });
    }
}
