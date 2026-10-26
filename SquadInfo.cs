using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SquadShot;

internal static class SquadInfo
{
    private static readonly string[] ProcessNames = { "SquadGame-Win64-Shipping", "SquadGame" };

    private static readonly Regex WorldRegex = new(@"Bringing World /Game/Maps/(?<path>\S+)", RegexOptions.Compiled);

    private static readonly Regex LoadingRegex = new(@"StartLoadingDestination to: /Game/Maps/(?<path>\S+)", RegexOptions.Compiled);

    private static readonly string[] TransientKeywords =
    {
        "TransitionMap", "Entry", "MainMenu", "LoadingScreen", "Startup", "Blank"
    };

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SquadGame", "Saved", "Logs", "SquadGame.log");

    /// <summary>Hunts for the visible Squad game window. Returns IntPtr.Zero when the game is not running.</summary>
    public static IntPtr FindGameWindow()
    {
        foreach (string name in ProcessNames)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(name);
            }
            catch (Exception ex)
            {
                Logger.Warn("枚举进程 " + name + " 失败：" + ex.Message);
                continue;
            }

            try
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        IntPtr handle = process.MainWindowHandle;
                        if (handle == IntPtr.Zero || !Native.IsWindowVisible(handle) || Native.IsIconic(handle)) continue;

                        var className = new StringBuilder(256);
                        Native.GetClassName(handle, className, className.Capacity);
                        Logger.Info("找到游戏窗口：" + name + " class=" + className + " hwnd=0x" + handle.ToInt64().ToString("X"));
                        return handle;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("读取窗口信息失败：" + ex.Message);
                    }
                }
            }
            finally
            {
                foreach (Process process in processes) process.Dispose();
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>Parses the newest map/layer the game loaded from SquadGame.log, e.g. "Gorodok_RAAS_v1".</summary>
    public static string? TryGetCurrentMap()
    {
        try
        {
            if (!File.Exists(LogPath)) return null;

            string tail;
            using (var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                long length = stream.Length;
                long start = Math.Max(0, length - 512 * 1024);
                stream.Seek(start, SeekOrigin.Begin);
                var buffer = new byte[length - start];
                int read = 0;
                while (read < buffer.Length)
                {
                    int count = stream.Read(buffer, read, buffer.Length - read);
                    if (count <= 0) break;
                    read += count;
                }
                tail = Encoding.UTF8.GetString(buffer, 0, read);
            }

            var candidates = new List<(int Index, string Value)>();
            foreach (Match match in WorldRegex.Matches(tail))
            {
                string? value = FormatMapPath(match.Groups["path"].Value);
                if (value != null) candidates.Add((match.Index, value));
            }
            foreach (Match match in LoadingRegex.Matches(tail))
            {
                string? value = FormatMapPath(match.Groups["path"].Value);
                if (value != null) candidates.Add((match.Index, value));
            }

            if (candidates.Count == 0) return null;
            return candidates.OrderBy(item => item.Index).Last().Value;
        }
        catch (Exception ex)
        {
            Logger.Warn("解析 Squad 日志失败：" + ex.Message);
            return null;
        }
    }

    private static string? FormatMapPath(string path)
    {
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return null;

        string mapName = segments[0];
        if (IsTransient(mapName)) return null;

        string layer = segments[^1].Split('.')[0];
        if (layer.Length == 0 || IsTransient(layer)) return null;

        if (layer.Equals(mapName, StringComparison.OrdinalIgnoreCase)) return Sanitize(mapName);
        if (layer.StartsWith(mapName + "_", StringComparison.OrdinalIgnoreCase)) return Sanitize(layer);
        return Sanitize(mapName + "_" + layer);
    }

    private static bool IsTransient(string value)
    {
        foreach (string keyword in TransientKeywords)
        {
            if (value.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }
        return value.Trim('\'', '\"');
    }
}
