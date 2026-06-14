using System.ComponentModel;
using System.Windows;
using Forms = System.Windows.Forms;
using OneThing90.Services;
using OneThing90.ViewModels;

namespace OneThing90;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _allowClose;
    private bool _hasShownTrayHint;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new StateStore(), new StartupService());
        DataContext = _viewModel;
        _viewModel.NotificationRequested += OnNotificationRequested;

        _notifyIcon = CreateNotifyIcon();
        StateChanged += OnStateChanged;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose && _viewModel.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            ShowTrayHint();
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.OnClosing(e);
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add("Открыть", null, (_, _) => Dispatcher.Invoke(ShowMainWindow));
        menu.Items.Add("Начать 90 минут", null, (_, _) => Dispatcher.Invoke(_viewModel.StartDefaultSessionFromTray));
        menu.Items.Add("Отложить на 30 минут", null, (_, _) => Dispatcher.Invoke(() => _viewModel.SnoozeFromTray(TimeSpan.FromMinutes(30))));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Выйти", null, (_, _) => Dispatcher.Invoke(ExitApplication));

        var notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "OneThing90",
            ContextMenuStrip = menu,
            Visible = true
        };

        notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        notifyIcon.BalloonTipClicked += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        return notifyIcon;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _viewModel.MinimizeToTray)
        {
            Hide();
            ShowTrayHint();
        }
    }

    private void OnNotificationRequested(object? sender, NotificationRequest request)
    {
        Dispatcher.Invoke(() =>
        {
            _notifyIcon.BalloonTipTitle = request.Title;
            _notifyIcon.BalloonTipText = request.Message;
            _notifyIcon.ShowBalloonTip(10_000);
        });
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ShowTrayHint()
    {
        if (_hasShownTrayHint)
        {
            return;
        }

        _hasShownTrayHint = true;
        _notifyIcon.BalloonTipTitle = "OneThing90 работает в трее";
        _notifyIcon.BalloonTipText = "Двойной клик по иконке рядом с часами вернет окно.";
        _notifyIcon.ShowBalloonTip(5000);
    }

    private void ExitApplication()
    {
        _allowClose = true;
        _notifyIcon.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }
}
