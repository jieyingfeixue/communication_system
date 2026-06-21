using System.Collections.ObjectModel;
using System.Windows;
using CommunicationSystem.Desktop.Services;
using CommunicationSystem.Shared.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunicationSystem.Desktop.ViewModels;

public partial class CreateGroupViewModel : ObservableObject
{
    [ObservableProperty] private string _groupName = "";
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<SelectableFriendItem> Friends { get; } = [];

    public CreateGroupViewModel(IEnumerable<FriendDto> friends)
    {
        foreach (var f in friends)
            Friends.Add(new SelectableFriendItem(f));
    }

    [RelayCommand]
    private async Task CreateAsync(Window? window)
    {
        if (string.IsNullOrWhiteSpace(GroupName))
        {
            StatusMessage = "请输入群名称";
            return;
        }

        var memberIds = Friends.Where(f => f.IsSelected).Select(f => f.Friend.UserId).ToList();
        var result = await ApiService.Instance.PostAsync("/api/groups",
            new CreateGroupRequest(GroupName.Trim(), memberIds));

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

public partial class SelectableFriendItem(FriendDto friend) : ObservableObject
{
    public FriendDto Friend { get; } = friend;
    [ObservableProperty] private bool _isSelected;
}
