using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using CommunicationSystem.Desktop.Services;
using CommunicationSystem.Desktop.Views;
using CommunicationSystem.Shared;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunicationSystem.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ChatHubService _hub;
    private readonly ApiService _api = ApiService.Instance;

    [ObservableProperty] private string _connectionStatus = "";
    [ObservableProperty] private string _chatTitle = "请选择会话";
    [ObservableProperty] private string _typingHint = "";
    [ObservableProperty] private string _messageInput = "";
    [ObservableProperty] private string _searchKeyword = "";
    [ObservableProperty] private string _historyKeyword = "";
    [ObservableProperty] private FriendDto? _selectedFriend;
    [ObservableProperty] private GroupDto? _selectedGroup;
    [ObservableProperty] private int _pendingRequestCount;

    private int? _loadedFriendId;
    private int? _loadedGroupId;
    private bool _suppressConversationReload;

    public bool IsGroupSelected => SelectedGroup is not null;
    public bool IsGroupOwner => SelectedGroup is not null &&
        SelectedGroup.OwnerId == _api.CurrentUser?.UserId;
    public bool IsGroupMemberNotOwner => IsGroupSelected && !IsGroupOwner;

    public ObservableCollection<FriendDto> Friends { get; } = [];
    public ObservableCollection<GroupDto> Groups { get; } = [];
    [ObservableProperty] private ObservableCollection<ChatMessageItem> _messages = new();
    public ObservableCollection<UserSummaryDto> SearchResults { get; } = [];
    public ObservableCollection<FriendRequestDto> FriendRequests { get; } = [];

    public event Action<bool>? RequestScrollToBottom;

    public string CurrentNickname => _api.CurrentUser?.Nickname ?? "";

    public MainViewModel(ChatHubService hub)
    {
        _hub = hub;
        _hub.MessageReceived += OnMessageReceived;
        _hub.UserTyping += n => RunOnUi(() => TypingHint = $"{n.Nickname} 正在输入...");
        _hub.UserOnline += id => _ = RefreshFriendsAsync();
        _hub.UserOffline += id => _ = RefreshFriendsAsync();
        _hub.FriendRequestReceived += () => _ = LoadRequestsAsync();
        _hub.FriendListChanged += () => _ = RefreshAllAsync();
        _hub.MessageDeleted += OnMessageDeleted;
        _hub.ChatHistoryCleared += OnChatHistoryCleared;
        _hub.GroupListChanged += () => _ = LoadGroupsAsync();
        _hub.GroupDissolved += OnGroupDissolved;
        _hub.ConnectionStateChanged += s => RunOnUi(() => ConnectionStatus = s);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshAllAsync();
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        await RefreshFriendsAsync();
        await LoadGroupsAsync();
        await LoadRequestsAsync();
        RunOnUi(() => ConnectionStatus = "已同步 " + DateTime.Now.ToString("HH:mm:ss"));
    }

    private static void RunOnUi(Action action)
    {
        var app = Application.Current;
        if (app?.Dispatcher is null) return;
        if (app.Dispatcher.CheckAccess())
            action();
        else
            app.Dispatcher.Invoke(action);
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }

    private void OnMessageDeleted(long messageId)
    {
        RunOnUi(() =>
        {
            var item = Messages.FirstOrDefault(m => m.Id == messageId);
            if (item is not null)
                Messages.Remove(item);
        });
    }

    private void OnChatHistoryCleared(int? friendId, int? groupId)
    {
        RunOnUi(() =>
        {
            if (SelectedFriend is not null && friendId == SelectedFriend.UserId)
                Messages.Clear();
            else if (SelectedGroup is not null && groupId == SelectedGroup.Id)
                Messages.Clear();
        });
    }

    private void OnMessageReceived(MessageDto msg)
    {
        RunOnUi(() =>
        {
            if (ShouldShowMessage(msg))
            {
                Messages.Add(ChatMessageItem.From(msg, _api.CurrentUser!.UserId, IsGroupOwner, SelectedGroup is not null));
                if (SelectedFriend is not null && msg.SenderId == SelectedFriend.UserId)
                    _ = _hub.MarkAsReadAsync(SelectedFriend.UserId);
                RequestScrollToBottom?.Invoke(false);
            }
            BumpFriendOnMessage(msg);
        });
    }

    private void BumpFriendOnMessage(MessageDto msg)
    {
        if (msg.GroupId.HasValue) return;
        var myId = _api.CurrentUser?.UserId;
        if (myId is null) return;

        int? friendId = null;
        var incrementUnread = false;
        if (msg.ReceiverId == myId)
        {
            friendId = msg.SenderId;
            incrementUnread = SelectedFriend?.UserId != friendId;
        }
        else if (msg.SenderId == myId && msg.ReceiverId.HasValue)
            friendId = msg.ReceiverId;

        if (!friendId.HasValue) return;

        var friend = Friends.FirstOrDefault(f => f.UserId == friendId.Value);
        if (friend is null)
        {
            _ = RefreshFriendsAsync();
            return;
        }

        var unread = incrementUnread
            ? friend.UnreadCount + 1
            : SelectedFriend?.UserId == friendId ? 0 : friend.UnreadCount;
        var updated = friend with { UnreadCount = unread, LastMessageTime = msg.CreateTime };
        Friends.Remove(friend);
        Friends.Insert(0, updated);
        if (SelectedFriend?.UserId == friendId)
        {
            _suppressConversationReload = true;
            SelectedFriend = updated;
            _suppressConversationReload = false;
        }
    }

    private bool ShouldShowMessage(MessageDto msg)
    {
        if (SelectedFriend is not null)
            return (msg.SenderId == SelectedFriend.UserId && msg.ReceiverId == _api.CurrentUser?.UserId) ||
                   (msg.SenderId == _api.CurrentUser?.UserId && msg.ReceiverId == SelectedFriend.UserId);
        if (SelectedGroup is not null)
            return msg.GroupId == SelectedGroup.Id;
        return false;
    }

    [RelayCommand]
    private async Task RefreshFriendsAsync()
    {
        var list = await _api.GetFriendsAsync() ?? [];
        var selectedId = SelectedFriend?.UserId;
        RunOnUi(() =>
        {
            ReplaceCollection(Friends, list);
            if (!selectedId.HasValue) return;
            var updated = Friends.FirstOrDefault(f => f.UserId == selectedId.Value);
            if (updated is not null && !ReferenceEquals(updated, SelectedFriend))
            {
                _suppressConversationReload = true;
                SelectedFriend = updated;
                _suppressConversationReload = false;
            }
        });
    }

    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        var list = await _api.GetGroupsAsync() ?? [];
        RunOnUi(() => ReplaceCollection(Groups, list));
    }

    [RelayCommand]
    private async Task LoadRequestsAsync()
    {
        var list = await _api.GetRequestsAsync() ?? [];
        RunOnUi(() =>
        {
            ReplaceCollection(FriendRequests, list);
            PendingRequestCount = list.Count;
        });
    }

    [RelayCommand]
    private async Task SearchUsersAsync()
    {
        var list = await _api.SearchUsersAsync(SearchKeyword) ?? [];
        RunOnUi(() => ReplaceCollection(SearchResults, list));
    }

    private void OnGroupDissolved(int groupId)
    {
        RunOnUi(() =>
        {
            if (SelectedGroup?.Id == groupId)
            {
                SelectedGroup = null;
                _loadedGroupId = null;
                Messages.Clear();
                ChatTitle = "请选择会话";
            }
            _ = LoadGroupsAsync();
        });
    }

    partial void OnSelectedFriendChanged(FriendDto? value)
    {
        OnPropertyChanged(nameof(IsGroupSelected));
        OnPropertyChanged(nameof(IsGroupOwner));
        OnPropertyChanged(nameof(IsGroupMemberNotOwner));
        if (value is null) return;

        SelectedGroup = null;
        ChatTitle = $"{value.Nickname} ({(value.IsOnline ? "在线" : "离线")})";
        if (_suppressConversationReload) return;

        if (_loadedFriendId != value.UserId)
        {
            _loadedFriendId = value.UserId;
            _loadedGroupId = null;
            _ = LoadMessagesAsync();
            _ = _hub.MarkAsReadAsync(value.UserId);
        }
    }

    partial void OnSelectedGroupChanged(GroupDto? value)
    {
        OnPropertyChanged(nameof(IsGroupSelected));
        OnPropertyChanged(nameof(IsGroupOwner));
        OnPropertyChanged(nameof(IsGroupMemberNotOwner));
        if (value is null) return;
        if (_suppressConversationReload) return;

        SelectedFriend = null;
        _loadedFriendId = null;
        ChatTitle = $"群聊: {value.GroupName} ({value.MemberCount}人)";
        if (_loadedGroupId != value.Id)
        {
            _loadedGroupId = value.Id;
            _ = LoadMessagesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadMessagesAsync()
    {
        var list = await _api.GetMessagesAsync(SelectedFriend?.UserId, SelectedGroup?.Id, HistoryKeyword) ?? [];
        var isGroup = SelectedGroup is not null;
        var isOwner = IsGroupOwner;
        var myId = _api.CurrentUser!.UserId;
        var items = list.OrderBy(x => x.CreateTime)
            .Select(m => ChatMessageItem.From(m, myId, isOwner, isGroup)).ToList();
        RunOnUi(() =>
        {
            Messages = new ObservableCollection<ChatMessageItem>(items);
            RequestScrollToBottom?.Invoke(true);
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;
        if (SelectedFriend is null && SelectedGroup is null) return;

        await _hub.SendMessageAsync(new SendMessageRequest(
            SelectedFriend?.UserId, SelectedGroup?.Id, MessageInput.Trim(), MessageType.Text));
        MessageInput = "";
    }

    [RelayCommand]
    private async Task SendFileAsync()
    {
        if (SelectedFriend is null && SelectedGroup is null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog();
        if (dlg.ShowDialog() != true) return;

        var upload = await _api.UploadFileAsync(dlg.FileName);
        if (upload is null) return;

        var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
        var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var type = imageExts.Contains(ext) ? MessageType.Image : MessageType.File;
        await _hub.SendMessageAsync(new SendMessageRequest(
            SelectedFriend?.UserId, SelectedGroup?.Id, upload.Url, type));
    }

    [RelayCommand]
    private async Task AddFriendAsync(UserSummaryDto? user)
    {
        if (user is null) return;
        var result = await _api.PostAsync("/api/friends/request", new AddFriendRequest(user.Id));
        RunOnUi(() => ConnectionStatus = result.Message);
    }

    [RelayCommand]
    private async Task AcceptFriendAsync(FriendRequestDto? req)
    {
        if (req is null) return;
        await _api.PostAsync("/api/friends/accept", new FriendshipActionRequest(req.FriendshipId));
        await LoadRequestsAsync();
        await RefreshFriendsAsync();
    }

    [RelayCommand]
    private async Task RejectFriendAsync(FriendRequestDto? req)
    {
        if (req is null) return;
        await _api.PostAsync("/api/friends/reject", new FriendshipActionRequest(req.FriendshipId));
        await LoadRequestsAsync();
    }

    [RelayCommand]
    private async Task DeleteFriendAsync(FriendDto? friend)
    {
        if (friend is null) return;
        await _api.DeleteAsync($"/api/friends/{friend.UserId}");
        await RefreshFriendsAsync();
    }

    [RelayCommand]
    private void OpenCreateGroupDialog()
    {
        if (Friends.Count == 0)
        {
            ConnectionStatus = "请先添加好友后再创建群聊";
            return;
        }

        var dialog = new CreateGroupWindow(Friends.ToList()) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
            _ = LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task ShowGroupMembersAsync()
    {
        if (SelectedGroup is null) return;
        var members = await _api.GetGroupMembersAsync(SelectedGroup.Id) ?? [];
        var names = string.Join("\n", members.Select(m => m.Nickname));
        MessageBox.Show(string.IsNullOrEmpty(names) ? "暂无成员" : names, "群成员", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task OpenInviteGroupDialogAsync()
    {
        if (SelectedGroup is null) return;
        var members = await _api.GetGroupMembersAsync(SelectedGroup.Id) ?? [];
        var inGroup = members.Select(m => m.UserId).ToHashSet();
        var candidates = Friends.Where(f => !inGroup.Contains(f.UserId)).ToList();
        if (candidates.Count == 0)
        {
            ConnectionStatus = "没有可邀请的好友";
            return;
        }

        var dialog = new InviteGroupWindow(SelectedGroup.Id, candidates) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
            await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task DissolveGroupAsync()
    {
        if (SelectedGroup is null) return;
        if (MessageBox.Show("确定解散该群聊？所有成员将无法继续群聊。", "解散群聊",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var result = await _api.DeleteAsync($"/api/groups/{SelectedGroup.Id}");
        ConnectionStatus = result.Message;
        if (result.Success)
        {
            SelectedGroup = null;
            Messages.Clear();
            ChatTitle = "请选择会话";
            await LoadGroupsAsync();
        }
    }

    [RelayCommand]
    private async Task LeaveGroupAsync()
    {
        if (SelectedGroup is null) return;
        if (MessageBox.Show("确定退出该群聊？", "退出群聊",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await _api.PostAsync("/api/groups/leave", new RemoveFromGroupRequest(SelectedGroup.Id, 0));
        ConnectionStatus = result.Message;
        if (result.Success)
        {
            SelectedGroup = null;
            Messages.Clear();
            ChatTitle = "请选择会话";
            await LoadGroupsAsync();
        }
    }

    [RelayCommand]
    private async Task RecallMessageAsync(ChatMessageItem? item)
    {
        if (item is null) return;
        var result = await _api.PostAsync($"/api/messages/{item.Id}/recall");
        if (!result.Success)
            ConnectionStatus = result.Message;
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(ChatMessageItem? item)
    {
        if (item is null) return;
        await _api.DeleteAsync($"/api/messages/{item.Id}");
        RunOnUi(() => Messages.Remove(item));
        // SignalR MessageDeleted will sync other clients; local remove is immediate
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        if (SelectedFriend is not null)
            await _api.DeleteAsync($"/api/messages/clear?friendId={SelectedFriend.UserId}");
        else if (SelectedGroup is not null)
            await _api.DeleteAsync($"/api/messages/clear?groupId={SelectedGroup.Id}");
        RunOnUi(() => Messages.Clear());
    }

    [RelayCommand]
    private async Task OnInputChangedAsync()
    {
        await _hub.NotifyTypingAsync(SelectedFriend?.UserId, SelectedGroup?.Id);
    }
}

public class ChatMessageItem
{
    public long Id { get; init; }
    public int SenderId { get; init; }
    public string SenderNickname { get; init; } = "";
    public string Content { get; init; } = "";
    public string DisplayContent { get; init; } = "";
    public DateTime CreateTime { get; init; }
    public bool IsMine { get; init; }
    public bool CanRecall { get; init; }
    public MessageType MessageType { get; init; }
    public ImageSource? ImageSource { get; init; }
    public string TimeText => CreateTime.ToLocalTime().ToString("HH:mm");
    public Visibility ShowSender => IsMine ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ShowText => MessageType == MessageType.Text ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowImage => MessageType == MessageType.Image ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowFile => MessageType == MessageType.File ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowRecall => CanRecall ? Visibility.Visible : Visibility.Collapsed;

    public static ChatMessageItem From(MessageDto m, int myUserId, bool isGroupOwner, bool isGroupChat) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderNickname = m.SenderNickname,
        Content = m.Content,
        DisplayContent = m.MessageType switch
        {
            MessageType.Image => "[图片]",
            MessageType.File => "[文件]",
            _ => m.Content
        },
        CreateTime = m.CreateTime,
        IsMine = m.SenderId == myUserId,
        CanRecall = CanRecallMessage(m, myUserId, isGroupOwner, isGroupChat),
        MessageType = m.MessageType,
        ImageSource = m.MessageType == MessageType.Image ? ImageCache.Get(m.Content) : null
    };

    private static bool CanRecallMessage(MessageDto m, int myUserId, bool isGroupOwner, bool isGroupChat)
    {
        if (isGroupChat && isGroupOwner) return true;
        if (m.SenderId != myUserId) return false;
        return DateTimeHelper.WithinMinutes(m.CreateTime, 2);
    }
}
