namespace SquadShot;

internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SquadShot");

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public static string LastCaptureFile => Path.Combine(DataDirectory, "last-capture.json");

    public static string DefaultSaveDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SquadShots");
}
