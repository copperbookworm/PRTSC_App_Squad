using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SquadShot;

internal static class ScreenCapture
{
    private const uint Srccopy = 0x00CC0020;
    private const uint Captureblt = 0x40000000;

    /// <summary>GDI BitBlt of a desktop rectangle. Works for Squad's borderless fullscreen and never includes the cursor.</summary>
    public static Bitmap CaptureRectangle(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentException("截图区域为空", nameof(bounds));
        }

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            IntPtr destination = graphics.GetHdc();
            try
            {
                IntPtr source = GetDC(IntPtr.Zero);
                if (source == IntPtr.Zero)
                {
                    throw new InvalidOperationException("无法获取屏幕设备上下文。");
                }

                try
                {
                    if (!BitBlt(destination, 0, 0, bounds.Width, bounds.Height, source, bounds.Left, bounds.Top, Srccopy | Captureblt))
                    {
                        throw new InvalidOperationException("BitBlt 截屏失败（Win32 错误 " + Marshal.GetLastWin32Error() + "）。");
                    }
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, source);
                }
            }
            finally
            {
                graphics.ReleaseHdc(destination);
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    /// <summary>PrintWindow fallback for windowed mode; returns null when the window cannot be rendered.</summary>
    public static Bitmap? CaptureWindow(IntPtr window)
    {
        if (window == IntPtr.Zero) return null;
        if (!Native.GetWindowRect(window, out Native.RECT rect)) return null;
        if (rect.Width <= 0 || rect.Height <= 0) return null;

        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            IntPtr hdc = graphics.GetHdc();
            bool ok;
            try
            {
                ok = Native.PrintWindow(window, hdc, Native.PW_RENDERFULLCONTENT);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            if (!ok)
            {
                bitmap.Dispose();
                return null;
            }
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            return null;
        }
    }

    public static Bitmap CreateThumbnail(Bitmap source, int maxWidth, int maxHeight)
    {
        double scale = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
        if (scale > 1.0) scale = 1.0;

        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(thumbnail);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return thumbnail;
    }

    /// <summary>Detects an all-black / single-color frame (exclusive fullscreen or a stale window surface).</summary>
    public static bool LooksBlank(Bitmap bitmap)
    {
        int stepX = Math.Max(1, bitmap.Width / 48);
        int stepY = Math.Max(1, bitmap.Height / 27);
        int min = int.MaxValue;
        int max = int.MinValue;

        for (int y = 0; y < bitmap.Height; y += stepY)
        {
            for (int x = 0; x < bitmap.Width; x += stepX)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int luminance = (pixel.R * 30 + pixel.G * 59 + pixel.B * 11) / 100;
                if (luminance < min) min = luminance;
                if (luminance > max) max = luminance;
            }
        }

        return max - min <= 4;
    }
}
