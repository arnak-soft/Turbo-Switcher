using System.Reflection;

namespace TypoSwitch;

internal static class AppVersion
{
    public static Version Current =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static string Display => Current.ToString(3);
}
