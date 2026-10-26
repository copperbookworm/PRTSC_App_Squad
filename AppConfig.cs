using System.Text.Json;

namespace SquadShot;

internal sealed class AppConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string SaveDirectory { get; set; } = AppPaths.DefaultSaveDirectory;

    public string ImageFormat { get; set; } = "png";

    public int JpegQuality { get; set; } = 92;

    /// <summary>monitor | window</summary>
    public string CaptureMode { get; set; } = "monitor";

    public bool IncludeMapName { get; set; } = true;

    public bool CopyToClipboard { get; set; }

    public bool PlaySound { get; set; } = true;

    public bool ShowToast { get; set; } = true;

    public int ToastSeconds { get; set; } = 3;

    public string Hotkey { get; set; } = "PrintScreen";

    public bool AutoStart { get; set; }

    public bool IsJpeg =>
        string.Equals(ImageFormat, "jpg", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ImageFormat, "jpeg", StringComparison.OrdinalIgnoreCase);

    public string EffectiveSaveDirectory
    {
        get
        {
            string value = (SaveDirectory ?? string.Empty).Trim();
            if (value.Length == 0) return AppPaths.DefaultSaveDirectory;
            try
            {
                value = Environment.ExpandEnvironmentVariables(value);
            }
            catch
            {
                return AppPaths.DefaultSaveDirectory;
            }
            return value;
        }
    }

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(AppPaths.ConfigFile))
            {
                AppConfig? loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(AppPaths.ConfigFile), JsonOptions);
                if (loaded != null) return loaded;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("读取配置失败，使用默认配置", ex);
        }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.WriteAllText(AppPaths.ConfigFile, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex)
        {
            Logger.Error("保存配置失败", ex);
        }
    }

    public AppConfig Clone() => (AppConfig)MemberwiseClone();
}
