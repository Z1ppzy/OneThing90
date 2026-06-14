using System.Diagnostics;
using Microsoft.Win32;

namespace OneThing90.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "OneThing90";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var configuredValue = key?.GetValue(ValueName) as string;
        return string.Equals(configuredValue, BuildRunValue(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(ValueName, BuildRunValue(), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string BuildRunValue()
    {
        var path = Process.GetCurrentProcess().MainModule?.FileName
            ?? Environment.ProcessPath
            ?? "OneThing90.exe";

        return $"\"{path}\"";
    }
}
