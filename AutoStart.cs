using Microsoft.Win32;
using System.Windows.Forms;

namespace SquadShot;

internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SquadShot";

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value && value.Length > 0;
        }
        catch (Exception ex)
        {
            Logger.Warn("读取开机自启失败：" + ex.Message);
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (enabled)
            {
                string? executable = Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(ValueName, "\"" + executable + "\" --tray");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }
            Logger.Info("开机自启：" + enabled);
        }
        catch (Exception ex)
        {
            Logger.Error("写入开机自启失败", ex);
        }
    }
}
