using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SquadShot;

internal sealed class CaptureResult : IDisposable
{
    public bool Success { get; init; }

    public string? FilePath { get; init; }

    public string? Error { get; init; }

    public Bitmap? Image { get; init; }

    public Bitmap? Preview { get; init; }

    public int Width => Image?.Width ?? 0;

    public int Height => Image?.Height ?? 0;

    public string? MapName { get; init; }

    public string Source { get; init; } = string.Empty;

    public Rectangle Bounds { get; init; }

    public long ElapsedMs { get; init; }

    public void Dispose()
    {
        Image?.Dispose();
        Preview?.Dispose();
    }
}

internal static class CaptureService
{
    public static CaptureResult Capture(AppConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        Bitmap? image = null;
        try
        {
            IntPtr gameWindow = SquadInfo.FindGameWindow();
            string source;
            Rectangle bounds;

            if (config.CaptureMode == "window" && gameWindow != IntPtr.Zero)
            {
                image = ScreenCapture.CaptureWindow(gameWindow);
                if (image != null && ScreenCapture.LooksBlank(image))
                {
                    image.Dispose();
                    image = null;
                }
            }

            if (image == null)
            {
                Rectangle monitor = gameWindow != IntPtr.Zero
                    ? Screen.FromHandle(gameWindow).Bounds
                    : Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                image = ScreenCapture.CaptureRectangle(monitor);
                bounds = monitor;
                source = gameWindow != IntPtr.Zero ? "显示器（游戏所在）" : "显示器（主屏）";
            }
            else
            {
                bounds = new Rectangle(0, 0, image.Width, image.Height);
                source = "游戏窗口";
            }

            string? mapName = config.IncludeMapName && gameWindow != IntPtr.Zero
                ? SquadInfo.TryGetCurrentMap()
                : null;

            string path = BuildFilePath(config, mapName);
            Save(image, path, config);
            Bitmap preview = ScreenCapture.CreateThumbnail(image, 340, 191);

            stopwatch.Stop();
            Logger.Info("截图成功：" + path + " (" + image.Width + "x" + image.Height + ", " + stopwatch.ElapsedMilliseconds + "ms, " + source + ")");
            return new CaptureResult
            {
                Success = true,
                FilePath = path,
                Image = image,
                Preview = preview,
                MapName = mapName,
                Source = source,
                Bounds = bounds,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            image?.Dispose();
            Logger.Error("截图失败", ex);
            stopwatch.Stop();
            return new CaptureResult
            {
                Success = false,
                Error = ex.Message,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private static string BuildFilePath(AppConfig config, string? mapName)
    {
        string directory = config.EffectiveSaveDirectory;
        Directory.CreateDirectory(directory);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string name = "Squad_" + stamp;
        if (!string.IsNullOrEmpty(mapName)) name += "_" + mapName;

        string extension = config.IsJpeg ? ".jpg" : ".png";
        string path = Path.Combine(directory, name + extension);
        int suffix = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(directory, name + "_" + suffix + extension);
            suffix++;
        }
        return path;
    }

    private static void Save(Bitmap image, string path, AppConfig config)
    {
        if (!config.IsJpeg)
        {
            image.Save(path, ImageFormat.Png);
            return;
        }

        ImageCodecInfo? codec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(item => item.FormatID == ImageFormat.Jpeg.Guid);
        if (codec == null)
        {
            image.Save(path, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(
            System.Drawing.Imaging.Encoder.Quality,
            (long)Math.Clamp(config.JpegQuality, 10, 100));
        image.Save(path, codec, parameters);
    }
}
