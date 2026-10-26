using System.Text.Json;

namespace SquadShot;

internal static class CliReport
{
    public static void Write(CaptureResult result)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            long bytes = 0;
            if (!string.IsNullOrEmpty(result.FilePath) && File.Exists(result.FilePath))
            {
                bytes = new FileInfo(result.FilePath).Length;
            }

            var payload = new
            {
                ok = result.Success,
                path = result.FilePath,
                width = result.Width,
                height = result.Height,
                map = result.MapName,
                source = result.Source,
                elapsedMs = result.ElapsedMs,
                error = result.Error,
                bytes,
                at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            File.WriteAllText(
                AppPaths.LastCaptureFile,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Logger.Error("写截图报告失败", ex);
        }
    }
}
