using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CommunicationSystem.Shared.DTOs;

namespace CommunicationSystem.Desktop.Services;

public class ApiService
{
    public static ApiService Instance { get; } = new();
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:5000") };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthResponse? CurrentUser { get; private set; }

    public void SetToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = null;
        else
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void SetUser(AuthResponse user)
    {
        CurrentUser = user;
        SetToken(user.Token);
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return (false, ReadMessage(body), null);
        var data = JsonSerializer.Deserialize<AuthResponse>(body, JsonOptions)!;
        SetUser(data);
        return (true, "登录成功", data);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string password, string nickname)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password, nickname));
        var body = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(body)) : (false, ReadMessage(body));
    }

    public Task<List<FriendDto>?> GetFriendsAsync() => GetAsync<List<FriendDto>>("/api/friends");
    public Task<List<GroupDto>?> GetGroupsAsync() => GetAsync<List<GroupDto>>("/api/groups");

    public Task<List<GroupMemberDto>?> GetGroupMembersAsync(int groupId) =>
        GetAsync<List<GroupMemberDto>>($"/api/groups/{groupId}/members");
    public Task<List<FriendRequestDto>?> GetRequestsAsync() => GetAsync<List<FriendRequestDto>>("/api/friends/requests");
    public Task<List<UserSummaryDto>?> SearchUsersAsync(string? keyword) =>
        string.IsNullOrWhiteSpace(keyword)
            ? GetAsync<List<UserSummaryDto>>("/api/users/search")
            : GetAsync<List<UserSummaryDto>>($"/api/users/search?keyword={Uri.EscapeDataString(keyword.Trim())}");

    public Task<List<MessageDto>?> GetMessagesAsync(int? friendId, int? groupId, string? keyword = null)
    {
        var url = "/api/messages?Page=1&PageSize=100";
        if (friendId.HasValue) url += $"&FriendId={friendId}";
        if (groupId.HasValue) url += $"&GroupId={groupId}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&Keyword={Uri.EscapeDataString(keyword)}";
        return GetAsync<List<MessageDto>>(url);
    }

    public async Task<(bool Success, string Message)> PostAsync(string url, object? body = null)
    {
        var response = body is null ? await _http.PostAsync(url, null) : await _http.PostAsJsonAsync(url, body);
        var text = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(text)) : (false, ReadMessage(text));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        var text = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(text)) : (false, ReadMessage(text));
    }

    public async Task<FileUploadResponse?> UploadFileAsync(string filePath)
    {
        await using var fs = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fs), "file", Path.GetFileName(filePath));
        var response = await _http.PostAsync("/api/files/upload", content);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<FileUploadResponse>(JsonOptions);
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private static string ReadMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? json;
        }
        catch { }
        return json;
    }
}
