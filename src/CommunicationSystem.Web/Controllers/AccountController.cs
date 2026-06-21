using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Web.Filters;
using CommunicationSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationSystem.Web.Controllers;

public class AccountController(ApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await api.LoginAsync(request);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        HttpContext.Session.SetAuth(result.Data!);
        return result.Data!.Role == Shared.Enums.UserRole.Admin
            ? RedirectToAction("Users", "Admin")
            : RedirectToAction("Index", "Chat");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await api.RegisterAsync(request);
        ViewBag.Message = result.Message;
        ViewBag.Success = result.Success;
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.ClearAuth();
        return RedirectToAction("Login");
    }
}
