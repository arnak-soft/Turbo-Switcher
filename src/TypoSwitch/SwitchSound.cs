using System.Media;
using System.Reflection;

namespace TypoSwitch;

internal static class SwitchSound
{
    public const string Windows = "windows";
    public const string Custom = "custom";

    private static readonly SoundPlayer? CustomPlayer = LoadCustom();

    public static void Play(bool enabled, string style)
    {
        if (!enabled) return;

        try
        {
            if (style == Custom && CustomPlayer is not null)
                CustomPlayer.Play();
            else
                SystemSounds.Asterisk.Play();
        }
        catch
        {
            // ignore playback errors
        }
    }

    private static SoundPlayer? LoadCustom()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("TypoSwitch.Assets.switch.wav");
        if (stream is null) return null;

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;
        return new SoundPlayer(buffer);
    }
}
