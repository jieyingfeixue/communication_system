using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using CommunicationSystem.Desktop.ViewModels;

namespace CommunicationSystem.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private DispatcherTimer? _scrollTimer;
    private bool _forceNextScroll;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.RequestScrollToBottom += ScheduleScrollToBottom;
    }

    private void ScheduleScrollToBottom(bool force)
    {
        if (force)
            _forceNextScroll = true;
        else if (!IsNearBottom())
            return;

        _scrollTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _scrollTimer.Stop();
        _scrollTimer.Tick -= OnScrollTimerTick;
        _scrollTimer.Tick += OnScrollTimerTick;
        _scrollTimer.Start();
    }

    private void OnScrollTimerTick(object? sender, EventArgs e)
    {
        _scrollTimer!.Stop();
        _scrollTimer.Tick -= OnScrollTimerTick;

        var force = _forceNextScroll;
        _forceNextScroll = false;
        if (!force && !IsNearBottom()) return;

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (_vm.Messages.Count == 0) return;
            MessageScrollViewer.ScrollToEnd();
        });
    }

    private bool IsNearBottom()
    {
        if (MessageScrollViewer.ScrollableHeight <= 0)
            return true;
        return MessageScrollViewer.VerticalOffset >= MessageScrollViewer.ScrollableHeight - 120;
    }

    private void Input_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
            vm.SendMessageCommand.Execute(null);
    }

    private void FileLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
