using CommunicationSystem.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace CommunicationSystem.Desktop.Services;

public class ChatHubService
{
    private HubConnection? _connection;

    public event Action<MessageDto>? MessageReceived;
    public event Action<TypingNotification>? UserTyping;
    public event Action<int>? UserOnline;
    public event Action<int>? UserOffline;
    public event Action? FriendRequestReceived;
    public event Action? FriendListChanged;
    public event Action<long>? MessageDeleted;
    public event Action<int?, int?>? ChatHistoryCleared;
    public event Action? GroupListChanged;
    public event Action<int>? GroupDissolved;
    public event Action<string>? ConnectionStateChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string token)
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/chat", options =>
                options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>("ReceiveMessage", msg => MessageReceived?.Invoke(msg));
        _connection.On<TypingNotification>("UserTyping", n => UserTyping?.Invoke(n));
        _connection.On<int>("UserOnline", id => UserOnline?.Invoke(id));
        _connection.On<int>("UserOffline", id => UserOffline?.Invoke(id));
        _connection.On<FriendRequestDto>("FriendRequestReceived", _ => FriendRequestReceived?.Invoke());
        _connection.On("FriendListChanged", () => FriendListChanged?.Invoke());
        _connection.On("GroupListChanged", () => GroupListChanged?.Invoke());
        _connection.On<int>("GroupDissolved", id => GroupDissolved?.Invoke(id));
        _connection.On<long>("MessageDeleted", id => MessageDeleted?.Invoke(id));
        _connection.On<int?, int?>("ChatHistoryCleared", (friendId, groupId) =>
            ChatHistoryCleared?.Invoke(friendId, groupId));
        _connection.Reconnecting += _ => { ConnectionStateChanged?.Invoke("正在重连..."); return Task.CompletedTask; };
        _connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke("已重新连接");
            FriendListChanged?.Invoke();
            return Task.CompletedTask;
        };
        _connection.Closed += _ => { ConnectionStateChanged?.Invoke("连接已断开"); return Task.CompletedTask; };

        await _connection.StartAsync();
        ConnectionStateChanged?.Invoke("已连接");
    }

    public Task SendMessageAsync(SendMessageRequest request) =>
        _connection!.InvokeAsync("SendMessage", request);

    public Task MarkAsReadAsync(int friendId) =>
        _connection!.InvokeAsync("MarkAsRead", friendId);

    public Task NotifyTypingAsync(int? receiverId, int? groupId) =>
        _connection!.InvokeAsync("NotifyTyping", receiverId, groupId);

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
            await _connection.StopAsync();
    }
}
