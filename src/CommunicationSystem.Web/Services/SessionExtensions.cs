using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Web.Services;

public static class SessionExtensions
{
    public static void SetAuth(this ISession session, AuthResponse auth)
    {
        session.SetString("Token", auth.Token);
        session.SetInt32("UserId", auth.UserId);
        session.SetString("Username", auth.Username);
        session.SetString("Nickname", auth.Nickname);
        session.SetString("Role", auth.Role.ToString());
    }

    public static void ClearAuth(this ISession session)
    {
        session.Remove("Token");
        session.Remove("UserId");
        session.Remove("Username");
        session.Remove("Nickname");
        session.Remove("Role");
    }

    public static bool IsLoggedIn(this ISession session) => !string.IsNullOrEmpty(session.GetString("Token"));

    public static bool IsAdmin(this ISession session) =>
        session.GetString("Role") == UserRole.Admin.ToString();
}
