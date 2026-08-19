using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TypoSwitch;

internal static class AppIcon
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create(bool enabled, bool hasUpdate = false)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);
        var fill = enabled ? Color.FromArgb(37, 99, 235) : Color.FromArgb(107, 114, 128);
        using (var path = RoundedRect(1, 1, 30, 30, 8))
        using (var brush = new SolidBrush(fill))
            g.FillPath(brush, path);
        using var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
        var text = "AЯ";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2 + 0.5f);

        if (hasUpdate)
        {
            using var dot = new SolidBrush(Color.FromArgb(59, 130, 246));
            g.FillEllipse(dot, 21, 1, 9, 9);
            using var ring = new Pen(Color.White, 1.5f);
            g.DrawEllipse(ring, 21, 1, 9, 9);
        }

        var handle = bmp.GetHicon();
        using var temp = Icon.FromHandle(handle);
        var clone = (Icon)temp.Clone();
        DestroyIcon(handle);
        return clone;
    }

    private static GraphicsPath RoundedRect(int x, int y, int width, int height, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
