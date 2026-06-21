using System.Security.Claims;
using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController(IWebHostEnvironment env, IConfiguration config) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "请选择文件" });

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var savedName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, savedName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        var baseUrl = config["FileBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl.TrimEnd('/')}/uploads/{savedName}";

        return Ok(new FileUploadResponse(file.FileName, url, file.Length));
    }
}
