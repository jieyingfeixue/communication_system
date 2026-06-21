using System.Windows;
using CommunicationSystem.Desktop.ViewModels;
using CommunicationSystem.Shared.DTOs;

namespace CommunicationSystem.Desktop.Views;

public partial class InviteGroupWindow : Window
{
    public InviteGroupWindow(int groupId, IEnumerable<FriendDto> candidates)
    {
        InitializeComponent();
        DataContext = new InviteGroupViewModel(groupId, candidates);
    }
}
