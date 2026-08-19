using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TypoSwitch;

internal static class AppIcon
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create(bool enabled)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        var fill = enabled ? Color.FromArgb(37, 99, 235) : Color.FromArgb(107, 114, 128);
        using (var brush = new SolidBrush(fill))
            g.FillEllipse(brush, 1, 1, 30, 30);
        using var font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
        var text = "AЯ";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2);

        var handle = bmp.GetHicon();
        using var temp = Icon.FromHandle(handle);
        var clone = (Icon)temp.Clone();
        DestroyIcon(handle);
        return clone;
    }
}
