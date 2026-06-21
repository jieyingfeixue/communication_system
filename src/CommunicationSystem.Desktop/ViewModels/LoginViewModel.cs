using System.Collections.ObjectModel;
using System.Windows;
using CommunicationSystem.Desktop.Services;
using CommunicationSystem.Desktop.Views;
using CommunicationSystem.Shared.DTOs;
using CommunicationSystem.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunicationSystem.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _nickname = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isRegisterMode;

    [RelayCommand]
    private async Task LoginAsync()
    {
        var result = await ApiService.Instance.LoginAsync(Username, Password);
        StatusMessage = result.Message;
        if (!result.Success) return;

        if (result.Data!.Role == UserRole.Admin)
        {
            StatusMessage = "管理员请使用 Web 端登录";
            return;
        }

        var hub = new ChatHubService();
        await hub.ConnectAsync(result.Data.Token);

        var main = new MainWindow(new MainViewModel(hub));
        Application.Current.MainWindow = main;
        main.Show();
        CloseCurrent();
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var result = await ApiService.Instance.RegisterAsync(Username, Password, Nickname);
        StatusMessage = result.Message;
        if (result.Success) IsRegisterMode = false;
    }

    [RelayCommand]
    private void ToggleMode() => IsRegisterMode = !IsRegisterMode;

    private static void CloseCurrent()
    {
        foreach (Window w in Application.Current.Windows)
        {
            if (w is LoginWindow)
            {
                w.Close();
                break;
            }
        }
    }
}
