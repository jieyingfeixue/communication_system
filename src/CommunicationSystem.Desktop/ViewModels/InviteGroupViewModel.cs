using System.Collections.ObjectModel;
using System.Windows;
using CommunicationSystem.Desktop.Services;
using CommunicationSystem.Shared.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunicationSystem.Desktop.ViewModels;

public partial class InviteGroupViewModel : ObservableObject
{
    private readonly int _groupId;

    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<SelectableFriendItem> Friends { get; } = [];

    public InviteGroupViewModel(int groupId, IEnumerable<FriendDto> candidates)
    {
        _groupId = groupId;
        foreach (var f in candidates)
            Friends.Add(new SelectableFriendItem(f));
    }

    [RelayCommand]
    private async Task InviteAsync(Window? window)
    {
        var userIds = Friends.Where(f => f.IsSelected).Select(f => f.Friend.UserId).ToList();
        if (userIds.Count == 0)
        {
            StatusMessage = "请选择要邀请的好友";
            return;
        }

        var result = await ApiService.Instance.PostAsync("/api/groups/invite",
            new InviteToGroupRequest(_groupId, userIds));

        if (!result.Success)
        {
            StatusMessage = result.Message;
            return;
        }

        if (window is not null)
        {
            window.DialogResult = true;
            window.Close();
        }
    }

    [RelayCommand]
    private static void Cancel(Window? window)
    {
        if (window is not null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}
