using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunicationSystem.Api.Data;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Entities;
using CommunicationSystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CommunicationSystem.Api.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<(bool Success, string Message, AuthResponse? Data)> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "用户名和密码不能为空", null);

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            return (false, "用户名已存在", null);

        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Nickname = string.IsNullOrWhiteSpace(request.Nickname) ? request.Username.Trim() : request.Nickname.Trim(),
            Status = UserStatus.Pending,
            Role = UserRole.User
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, "注册成功，请等待管理员审批", null);
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "用户名或密码错误", null);

        if (user.Status == UserStatus.Pending)
            return (false, "账号待审批，请等待管理员通过", null);

        if (user.Status == UserStatus.Disabled)
            return (false, "账号已被禁用", null);

        return (true, "登录成功", CreateAuthResponse(user));
    }

    public AuthResponse CreateAuthResponse(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(7);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("nickname", user.Nickname)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Id,
            user.Username,
            user.Nickname,
            user.Avatar,
            user.Role,
            user.Status);
    }
}
