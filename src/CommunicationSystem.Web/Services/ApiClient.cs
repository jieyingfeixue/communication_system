using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CommunicationSystem.Shared.DTOs;

namespace CommunicationSystem.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http, IHttpContextAccessor httpContext, IConfiguration config)
    {
        _http = http;
        _httpContext = httpContext;
        _config = config;
        _http.BaseAddress = new Uri(_config["ApiBaseUrl"] ?? "http://localhost:5000");
    }

    private void ApplyAuth()
    {
        var token = _httpContext.HttpContext?.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return (false, ReadMessage(body), null);

        var data = JsonSerializer.Deserialize<AuthResponse>(body, JsonOptions)!;
        return (true, "登录成功", data);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(body)) : (false, ReadMessage(body));
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        ApplyAuth();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<(bool Success, string Message)> PostAsync(string url, object? body = null)
    {
        ApplyAuth();
        var response = body is null
            ? await _http.PostAsync(url, null)
            : await _http.PostAsJsonAsync(url, body);
        var text = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(text)) : (false, ReadMessage(text));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string url)
    {
        ApplyAuth();
        var response = await _http.DeleteAsync(url);
        var text = await response.Content.ReadAsStringAsync();
        return response.IsSuccessStatusCode ? (true, ReadMessage(text)) : (false, ReadMessage(text));
    }

    public string GetToken() => _httpContext.HttpContext?.Session.GetString("Token") ?? "";

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
