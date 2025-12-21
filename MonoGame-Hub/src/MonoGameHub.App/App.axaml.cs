using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MonoGameHub.App.Services;
using MonoGameHub.App.ViewModels;
using MonoGameHub.App.Views;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;
using System.Diagnostics;

namespace MonoGameHub.App;

public sealed partial class App : Application
{
    // Minimum time the splash screen should remain visible.
    // Adjust this to taste (e.g. 1000–2000ms).
    private static readonly TimeSpan MinSplashStartTime = TimeSpan.FromMilliseconds(1200);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppServices.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = AppServices.Services.GetRequiredService<SettingsStore>();

            // Debug/testing option to reset cached state.
            // Note: AlwaysClearCacheInDev is intended for local debugging only.
            if (StartupOptions.ClearCacheRequested || StartupOptions.AlwaysClearCacheInDev)
            {
                CleanCache(settingsStore, AppServices.Services);
            }

            EnsureDefaultNuGetPackagesFolderOnFirstLaunch(settingsStore);

            var settings = settingsStore.Load();
            if (settings.SkipSplashScreen)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = AppServices.Services.GetRequiredService<MainWindowViewModel>()
                };

                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                // Even when skipping the splash, still check for announcements and show a toast in-app.
                _ = Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var announcementTask = TryCheckAnnouncementAsync(AppServices.Services, settingsStore);

                    var finished = await Task.WhenAny(announcementTask, Task.Delay(TimeSpan.FromSeconds(5)));
                    if (finished == announcementTask)
                    {
                        var (isNew, post, thumb) = await announcementTask;
                        if (isNew && post is not null)
                            AppAnnouncementBus.Publish(post, thumb);
                        return;
                    }

                    _ = announcementTask.ContinueWith(t =>
                    {
                        if (t.Status != TaskStatus.RanToCompletion)
                            return;

                        var (isNew, post, thumb) = t.Result;
                        if (!isNew || post is null)
                            return;

                        Dispatcher.UIThread.Post(() => AppAnnouncementBus.Publish(post, thumb));
                    }, TaskScheduler.Default);
                });

                base.OnFrameworkInitializationCompleted();
                return;
            }

            var os = AppServices.Services.GetRequiredService<OsLauncher>();
            var splashVm = new SplashViewModel(os);
            var splash = new SplashWindow
            {
                DataContext = splashVm
            };

            var isTransitioningToMainWindow = false;
            EventHandler<WindowClosingEventArgs>? splashClosingHandler = null;
            splashClosingHandler = (_, _) =>
            {
                if (isTransitioningToMainWindow)
                    return;

                StartupOptions.AbortStartupRequested = true;
                desktop.Shutdown();
            };

            splash.Closing += splashClosingHandler;

            desktop.MainWindow = splash;

            // Keep UX snappy: show splash immediately, then build the main window while it is visible.
            Dispatcher.UIThread.Post(async () =>
            {
                if (StartupOptions.AbortStartupRequested)
                    return;

                var timer = Stopwatch.StartNew();

                // High-level splash hints (these will be followed by detailed logs from the real work).
                LogBus.Publish("Getting MonoGame dotnet templates...");
                LogBus.Publish("Getting the latest news...");
                LogBus.Publish("Searching for new resources...");

                // Best-effort: check for new announcements while the splash is visible.
                var announcementTask = TryCheckAnnouncementAsync(AppServices.Services, settingsStore);

                var mainWindow = new MainWindow
                {
                    DataContext = AppServices.Services.GetRequiredService<MainWindowViewModel>()
                };

                try
                {
                    // Don't let announcement checks hang startup indefinitely.
                    var finished = await Task.WhenAny(announcementTask, Task.Delay(TimeSpan.FromSeconds(5)));
                    if (finished == announcementTask)
                    {
                        var (isNew, post, thumb) = await announcementTask;
                        if (isNew && post is not null)
                        {
                            AppAnnouncementBus.Publish(post, thumb);
                        }
                    }
                    else
                    {
                        _ = announcementTask.ContinueWith(t =>
                        {
                            if (t.Status != TaskStatus.RanToCompletion)
                                return;

                            var (isNew, post, thumb) = t.Result;
                            if (!isNew || post is null)
                                return;

                            Dispatcher.UIThread.Post(() =>
                            {
                                // Always publish so the main window can toast.
                                AppAnnouncementBus.Publish(post, thumb);
                            });
                        }, TaskScheduler.Default);
                    }
                }
                catch
                {
                }

                if (StartupOptions.AbortStartupRequested)
                    return;

                // Ensure the splash is visible long enough to be meaningful.
                // Announcements now remain visible until the splash closes, so no extra time is needed.
                var remaining = MinSplashStartTime - timer.Elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining);

                if (StartupOptions.AbortStartupRequested)
                    return;

                isTransitioningToMainWindow = true;
                splash.Closing -= splashClosingHandler;
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                splash.Close();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void EnsureDefaultNuGetPackagesFolderOnFirstLaunch(SettingsStore settingsStore)
    {
        // First launch is defined as: no settings file yet.
        if (settingsStore.SettingsFileExists)
            return;

        // If the user has explicitly configured NuGet's global-packages folder via env var,
        // don't override it with an app setting.
        var env = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Environment.GetEnvironmentVariable("NUGET_PACKAGES", EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable("NUGET_PACKAGES", EnvironmentVariableTarget.Machine);

        if (!string.IsNullOrWhiteSpace(env))
            return;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
            return;

        var defaultGlobalPackagesFolder = Path.Combine(userProfile, ".nuget", "packages");

        var current = settingsStore.Load();
        if (!string.IsNullOrWhiteSpace(current.NuGetPackagesFolder))
            return;

        settingsStore.Save(current with { NuGetPackagesFolder = defaultGlobalPackagesFolder });
        LogBus.Publish($"First launch: defaulted NuGet packages folder to {defaultGlobalPackagesFolder}");
    }

    private static void CleanCache(SettingsStore settingsStore, IServiceProvider services)
    {
        LogBus.Publish("ClearCache: clearing caches and settings...");

        // Full reset: delete the entire %AppData%\MonoGameHub folder (settings + cache + any other persisted state).
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var expectedBaseDir = Path.Combine(appData, "MonoGameHub");

            var settingsDir = Path.GetDirectoryName(settingsStore.SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(settingsDir)
                && string.Equals(
                    Path.GetFullPath(settingsDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(expectedBaseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(settingsDir))
                    Directory.Delete(settingsDir, recursive: true);

                // Recreate base directory so subsequent SettingsStore.Save calls have a valid path.
                Directory.CreateDirectory(settingsDir);
            }
        }
        catch
        {
            // Best-effort.
        }

        // Reset in-memory settings cache to defaults.
        settingsStore.Clear();

        AppAnnouncementBus.Clear();

        try
        {
            services.GetRequiredService<MonoGameContentClient>().InvalidateCache();
        }
        catch
        {
            // Ignore.
        }

        try
        {
            services.GetRequiredService<TemplatePackState>().ReloadFromSettings();
        }
        catch
        {
            // Ignore.
        }
    }

    private static async Task<(bool IsNew, BlogPost? Post, IImage? Thumbnail)> TryCheckAnnouncementAsync(IServiceProvider services, SettingsStore settingsStore)
    {
        try
        {
            LogBus.Publish("Checking announcements...");

            var content = services.GetRequiredService<MonoGameContentClient>();
            var images = services.GetRequiredService<RemoteImageLoader>();

            var currentSettings = settingsStore.Load();
            var lastCount = currentSettings.LastSeenBlogPostCount;

            IReadOnlyList<BlogPost> posts;
            try
            {
                posts = await content.GetBlogPostsAsync(CancellationToken.None);
            }
            catch
            {
                return (false, null, null);
            }

            var newCount = posts.Count;

            // Always update the cached count so we don't show the same announcement again next run.
            if (newCount != currentSettings.LastSeenBlogPostCount)
                settingsStore.Save(currentSettings with { LastSeenBlogPostCount = newCount });

            // First run behavior:
            // If there are posts and the cached count is 0 (initial default), treat it as "new"
            // so users see the feature working immediately.

            if (posts.Count == 0)
                return (false, null, null);

            if (newCount == lastCount)
                return (false, null, null);

            var latest = posts[0];
            IImage? thumb = null;

            try
            {
                // Best-effort image load; toast still works without it.
                thumb = await images.GetAsync(latest.ImageUrl, CancellationToken.None);
            }
            catch
            {
                thumb = null;
            }

            return (true, latest, thumb);
        }
        catch
        {
            return (false, null, null);
        }
    }
}

