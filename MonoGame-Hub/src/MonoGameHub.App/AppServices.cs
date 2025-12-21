using Microsoft.Extensions.DependencyInjection;
using MonoGameHub.App.Services;
using MonoGameHub.App.ViewModels;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App;

public static class AppServices
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        var services = new ServiceCollection();

        services.AddSingleton<SettingsStore>();
        services.AddSingleton<DotNetCli>();
        services.AddSingleton<DotNetWorkloadManager>();
        services.AddSingleton<DotNetWorkloadState>();
        services.AddSingleton<NuGetVersionResolver>();
        services.AddSingleton<ProjectScanner>();
        services.AddSingleton<TemplateManager>();
        services.AddSingleton<TemplateWorkloadRegistry>();
        services.AddSingleton<MonoGameToolingVersionSync>();
        services.AddSingleton<OsLauncher>();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<RemoteImageLoader>();
        services.AddSingleton<MonoGameContentClient>();

        services.AddSingleton<TemplatePackState>();
        services.AddSingleton<ToolingSetupState>();

        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<InstallsViewModel>();
        services.AddTransient<NewProjectViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BlogViewModel>();
        services.AddTransient<ResourcesViewModel>();
        services.AddSingleton<MonoGameHub.Core.Services.DocsSiteCache>();
        services.AddTransient<DocsApiViewModel>();
        services.AddTransient<MainWindowViewModel>();

        Services = services.BuildServiceProvider();
        _initialized = true;
    }
}
