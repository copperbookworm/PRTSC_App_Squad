namespace SquadShot;

internal static class Logger
{
    private static readonly object Gate = new();

    public static string LogDirectory { get; } = Path.Combine(AppPaths.DataDirectory, "logs");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : message + " :: " + ex.GetType().Name + ": " + ex.Message);

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                string file = Path.Combine(LogDirectory, "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message + Environment.NewLine);
            }
        }
        catch
        {
            // logging must never take the app down
        }
    }
}
