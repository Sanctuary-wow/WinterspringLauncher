using System;
using System.Globalization;

namespace WinterspringLauncher;

public static class LocaleDefaults
{
    public static bool ShouldUseAsiaPreferences { get; set; } = false;

    public static string GetBestWoWConfigLocale()
    {
        return "enUS";
    }

    public static string? GetBestGitHubMirror()
    {
        return ShouldUseAsiaPreferences ? null : null;
    }

    public static string GetBestServerName()
    {
        return "Sanctuary";
    }
}
