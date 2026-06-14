namespace OneThing90.Core;

public sealed class AppSettings
{
    public bool RemindersEnabled { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool LaunchAtLogin { get; set; }
    public string ThemeMode { get; set; } = "System";
    public string AccentColor { get; set; } = "Teal";
    public string DensityMode { get; set; } = "Comfortable";
    public string CustomAccentHex { get; set; } = "#0D9488";
    public bool UseCustomWindowColor { get; set; }
    public string CustomWindowHex { get; set; } = "#202020";
    public int ReminderStartHour { get; set; } = 19;
    public int ReminderStartMinute { get; set; }
    public int ReminderEndHour { get; set; } = 23;
    public int ReminderEndMinute { get; set; }
    public int ReminderIntervalMinutes { get; set; } = 30;
}
