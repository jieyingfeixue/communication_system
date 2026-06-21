using System.Windows;
using CommunicationSystem.Desktop.ViewModels;
using CommunicationSystem.Shared.DTOs;

namespace CommunicationSystem.Desktop.Views;

public partial class CreateGroupWindow : Window
{
    public CreateGroupWindow(IEnumerable<FriendDto> friends)
    {
        InitializeComponent();
        DataContext = new CreateGroupViewModel(friends);
    }
}
