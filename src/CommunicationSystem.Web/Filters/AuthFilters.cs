using CommunicationSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CommunicationSystem.Web.Filters;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsLoggedIn())
            context.Result = new RedirectToActionResult("Login", "Account", null);
    }
}

public class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsLoggedIn())
            context.Result = new RedirectToActionResult("Login", "Account", null);
        else if (!session.IsAdmin())
            context.Result = new RedirectToActionResult("Index", "Chat", null);
    }
}
