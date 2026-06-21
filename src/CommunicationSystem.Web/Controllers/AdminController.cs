using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;
using CommunicationSystem.Web.Filters;
using CommunicationSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Web.Controllers;

[RequireAdmin]
public class AdminController(ApiClient api) : Controller
{
    public IActionResult Users() => View();

    public IActionResult Messages() => View();

    [HttpGet]
    public async Task<IActionResult> UserList(string? username, UserStatus? status)
    {
        var url = "/api/admin/users?";
        if (!string.IsNullOrWhiteSpace(username)) url += $"username={Uri.EscapeDataString(username)}&";
        if (status.HasValue) url += $"status={(int)status.Value}";
        return Json(await api.GetAsync<List<UserSummaryDto>>(url) ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int userId, bool approve = true)
    {
        var result = await api.PostAsync($"/api/admin/users/{userId}/approve?approve={approve.ToString().ToLower()}");
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Disable(int userId)
    {
        var result = await api.PostAsync($"/api/admin/users/{userId}/disable");
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Enable(int userId)
    {
        var result = await api.PostAsync($"/api/admin/users/{userId}/enable");
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> MessageList(string? keyword, int? senderId)
    {
        var url = "/api/admin/messages?Page=1&PageSize=100";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (senderId.HasValue) url += $"&senderId={senderId}";
        return Json(await api.GetAsync<List<MessageDto>>(url) ?? []);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMessage(long id) =>
        Json(await api.DeleteAsync($"/api/admin/messages/{id}"));
}
