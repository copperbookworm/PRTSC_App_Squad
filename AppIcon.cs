using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SquadShot;

internal static class AppIcon
{
    private const string ResourceName = "SquadShot.app.ico";

    private static Icon? _master;

    /// <summary>Returns a fresh copy of the application icon; the caller owns the returned instance.</summary>
    public static Icon Create()
    {
        _master ??= LoadMaster();
        return (Icon)_master.Clone();
    }

    private static Icon LoadMaster()
    {
        try
        {
            using Stream? stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName);
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("读取内置图标失败，改用运行时绘制", ex);
        }

        return DrawMaster();
    }

    /// <summary>Fallback: draws the same camera-ring mark at runtime when the embedded .ico is unavailable.</summary>
    private static Icon DrawMaster()
    {
        using var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var background = new SolidBrush(Color.FromArgb(255, 24, 26, 30));
            graphics.FillEllipse(background, 1, 1, 62, 62);

            using var ring = new Pen(Color.FromArgb(255, 88, 166, 255), 5f);
            graphics.DrawEllipse(ring, 15, 17, 34, 34);

            using var lens = new SolidBrush(Color.FromArgb(255, 88, 166, 255));
            graphics.FillEllipse(lens, 25, 27, 14, 14);

            using var notch = new SolidBrush(Color.FromArgb(255, 88, 166, 255));
            graphics.FillRectangle(notch, 25, 8, 14, 6);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
