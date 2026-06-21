using System.Windows;
using CommunicationSystem.Desktop.ViewModels;

namespace CommunicationSystem.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = new LoginViewModel();
    }

    private void PwdBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PwdBox.Password;
    }
}
