using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using OneThing90.Services;
using OneThing90.ViewModels;
using Media = System.Windows.Media;

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
        _viewModel.AppearanceChanged += (_, _) => ApplyAppearance();
        ApplyAppearance();
        ApplyWindowIcon();

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
            Icon = LoadAppIcon(),
            Text = "OneThing90",
            ContextMenuStrip = menu,
            Visible = true
        };

        notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        notifyIcon.BalloonTipClicked += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        return notifyIcon;
    }

    private void ApplyWindowIcon()
    {
        using var icon = LoadAppIcon();
        Icon = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(32, 32));
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
        {
            var icon = System.Drawing.Icon.ExtractAssociatedIcon(processPath);
            if (icon is not null)
            {
                return icon;
            }
        }

        return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
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

    private void ApplyAppearance()
    {
        var useLight = _viewModel.ThemeMode switch
        {
            "Light" => true,
            "Dark" => false,
            _ => IsWindowsLightTheme()
        };

        ApplyBaseTheme(useLight);
        ApplyCustomWindowColorIfNeeded();
        ApplyAccent(_viewModel.AccentColor, useLight);
        ApplyDensity(_viewModel.DensityMode);
    }

    private void ApplyBaseTheme(bool useLight)
    {
        if (useLight)
        {
            SetBrush("WindowBrush", "#F3F3F3");
            SetBrush("SidebarBrush", "#F7F7F7");
            SetBrush("SurfaceBrush", "#FFFFFF");
            SetBrush("SurfaceAltBrush", "#F8F8F8");
            SetBrush("InputBrush", "#FFFFFF");
            SetBrush("ButtonBrush", "#FFFFFF");
            SetBrush("ButtonHoverBrush", "#F1F1F1");
            SetBrush("ButtonPressedBrush", "#E9E9E9");
            SetBrush("LineBrush", "#DADADA");
            SetBrush("TextBrush", "#1F1F1F");
            SetBrush("MutedTextBrush", "#666666");
            SetBrush("BannerBrush", "#FFF7E8");
            SetBrush("BannerLineBrush", "#E5B567");
            SetBrush("BannerTitleBrush", "#5F3B00");
            SetBrush("BannerTextBrush", "#6F4B12");
            SetBrush("CalendarEmptyBrush", "#F5F5F5");
            SetBrush("CalendarMissedBrush", "#FDE7EC");
            SetBrush("CalendarFutureBrush", "#F0F0F0");
        }
        else
        {
            SetBrush("WindowBrush", "#202020");
            SetBrush("SidebarBrush", "#181818");
            SetBrush("SurfaceBrush", "#252526");
            SetBrush("SurfaceAltBrush", "#2D2D30");
            SetBrush("InputBrush", "#1E1E1E");
            SetBrush("ButtonBrush", "#2D2D30");
            SetBrush("ButtonHoverBrush", "#3E3E42");
            SetBrush("ButtonPressedBrush", "#242426");
            SetBrush("LineBrush", "#3F3F46");
            SetBrush("TextBrush", "#F3F3F3");
            SetBrush("MutedTextBrush", "#C7C7C7");
            SetBrush("BannerBrush", "#332A19");
            SetBrush("BannerLineBrush", "#8A621D");
            SetBrush("BannerTitleBrush", "#FFE8B0");
            SetBrush("BannerTextBrush", "#F8D99C");
            SetBrush("CalendarEmptyBrush", "#303036");
            SetBrush("CalendarMissedBrush", "#3A2028");
            SetBrush("CalendarFutureBrush", "#2A2A2F");
        }

        SetBrush("CalendarCompleteBrush", "#16A34A");
        SetBrush("CalendarCompleteLineBrush", "#86EFAC");
        SetBrush("CalendarTodayBrush", "#B45309");
        SetBrush("CalendarTodayLineBrush", "#FBBF24");
        SetBrush("CalendarPartialBrush", "#7C3AED");
        SetBrush("CalendarPartialLineBrush", "#C4B5FD");
        SetBrush("CalendarMissedLineBrush", "#FB7185");
    }

    private void ApplyAccent(string accent, bool useLight)
    {
        var (primary, primaryDark, primarySoft) = accent switch
        {
            "Custom" when TryParseColor(_viewModel.CustomAccentHex, out var customColor) => BuildCustomAccent(customColor, useLight),
            "Blue" => ("#2563EB", "#1D4ED8", useLight ? "#E8F0FF" : "#17233F"),
            "Green" => ("#16A34A", "#15803D", useLight ? "#E9F8EE" : "#172F20"),
            "Violet" => ("#7C3AED", "#6D28D9", useLight ? "#F0EAFE" : "#241B3A"),
            "Amber" => ("#D97706", "#B45309", useLight ? "#FFF4D6" : "#332712"),
            "Rose" => ("#E11D48", "#BE123C", useLight ? "#FDE8EE" : "#3A1720"),
            _ => ("#0D9488", "#0F766E", useLight ? "#E6F7F5" : "#162E2B")
        };

        SetBrush("PrimaryBrush", primary);
        SetBrush("PrimaryDarkBrush", primaryDark);
        SetBrush("PrimarySoftBrush", primarySoft);
    }

    private void ApplyCustomWindowColorIfNeeded()
    {
        if (!_viewModel.UseCustomWindowColor || !TryParseColor(_viewModel.CustomWindowHex, out var baseColor))
        {
            return;
        }

        var isLight = GetLuminance(baseColor) >= 0.56;
        var text = isLight ? "#1F1F1F" : "#F3F3F3";
        var muted = isLight ? "#666666" : "#C7C7C7";

        SetBrush("WindowBrush", ToHex(baseColor));
        SetBrush("SidebarBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.48) : Blend(baseColor, Media.Colors.Black, 0.28)));
        SetBrush("SurfaceBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.68) : Blend(baseColor, Media.Colors.White, 0.08)));
        SetBrush("SurfaceAltBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.82) : Blend(baseColor, Media.Colors.White, 0.13)));
        SetBrush("InputBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.86) : Blend(baseColor, Media.Colors.Black, 0.18)));
        SetBrush("ButtonBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.76) : Blend(baseColor, Media.Colors.White, 0.12)));
        SetBrush("ButtonHoverBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.60) : Blend(baseColor, Media.Colors.White, 0.20)));
        SetBrush("ButtonPressedBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.Black, 0.08) : Blend(baseColor, Media.Colors.Black, 0.18)));
        SetBrush("LineBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.Black, 0.18) : Blend(baseColor, Media.Colors.White, 0.18)));
        SetBrush("TextBrush", text);
        SetBrush("MutedTextBrush", muted);
        SetBrush("CalendarEmptyBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.70) : Blend(baseColor, Media.Colors.White, 0.10)));
        SetBrush("CalendarFutureBrush", ToHex(isLight ? Blend(baseColor, Media.Colors.White, 0.62) : Blend(baseColor, Media.Colors.Black, 0.14)));
    }

    private static (string Primary, string PrimaryDark, string PrimarySoft) BuildCustomAccent(Media.Color color, bool useLight)
    {
        var primaryDark = Blend(color, Media.Colors.Black, 0.22);
        var primarySoft = useLight
            ? Blend(color, Media.Colors.White, 0.86)
            : Blend(color, Media.Colors.Black, 0.68);

        return (ToHex(color), ToHex(primaryDark), ToHex(primarySoft));
    }

    private void ApplyDensity(string density)
    {
        var (sectionPadding, controlPadding, navPadding, rowGap, timerFontSize) = density switch
        {
            "Compact" => (14.0, new Thickness(10, 6, 10, 6), new Thickness(12, 8, 12, 8), 10.0, 58.0),
            "Spacious" => (26.0, new Thickness(16, 11, 16, 11), new Thickness(18, 13, 18, 13), 18.0, 72.0),
            _ => (20.0, new Thickness(14, 9, 14, 9), new Thickness(16, 10, 16, 10), 14.0, 64.0)
        };

        Resources["SectionPadding"] = new Thickness(sectionPadding);
        Resources["ControlPadding"] = controlPadding;
        Resources["NavPadding"] = navPadding;
        Resources["RowGap"] = rowGap;
        Resources["TimerFontSize"] = timerFontSize;
    }

    private static bool IsWindowsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var rawValue = key?.GetValue("AppsUseLightTheme");
        return rawValue is not int value || value != 0;
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(color));
    }

    private static bool TryParseColor(string? value, out Media.Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hex = value.Trim();
        if (!hex.StartsWith('#'))
        {
            hex = $"#{hex}";
        }

        try
        {
            color = (Media.Color)Media.ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Media.Color Blend(Media.Color from, Media.Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Media.Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private static double GetLuminance(Media.Color color)
    {
        static double Channel(byte value)
        {
            var normalized = value / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    private static string ToHex(Media.Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
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
