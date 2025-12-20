using System;

namespace MonoGameHub.App.Services;

public static class StartupOptions
{
    // Development/testing helper.
    // Set to true to always clear announcement-related caches on startup
    // (regardless of command-line options). Intended for local debugging only.
    public const bool AlwaysClearCacheInDev = true;

    // How long the in-app (main window) announcement toast stays visible.
    // Keep this relatively short so it doesn't distract from initial usage.
    public static readonly TimeSpan InAppAnnouncementToastDuration = TimeSpan.FromSeconds(15);

    public static bool ClearCacheRequested { get; set; }
}
