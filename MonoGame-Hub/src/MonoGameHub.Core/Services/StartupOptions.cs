namespace MonoGameHub.Core.Services;

public static class StartupOptions
{
    // Development/testing helper.
    // Set to true to force a full reset on every startup (settings + caches),
    // regardless of command-line options. Intended for local debugging only.
    public const bool AlwaysClearCacheInDev = true;

    // How long the in-app (main window) announcement toast stays visible.
    // Keep this relatively short so it doesn't distract from initial usage.
    public static readonly TimeSpan InAppAnnouncementToastDuration = TimeSpan.FromSeconds(15);

    public static bool ClearCacheRequested { get; set; }

    // When set, the app should abort startup (e.g., user closed the splash screen).
    public static bool AbortStartupRequested { get; set; }
}
