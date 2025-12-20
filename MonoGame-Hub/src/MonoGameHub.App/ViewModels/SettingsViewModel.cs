using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Services;
using System.Linq;

namespace MonoGameHub.App.ViewModels;

public sealed partial class SettingsViewModel : LoggableViewModel
{
    private readonly SettingsStore _settingsStore;
    private readonly DotNetCli _dotnet;
    private readonly OsLauncher _os;

    public SettingsViewModel(SettingsStore settingsStore, DotNetCli dotnet, OsLauncher os)
    {
        _settingsStore = settingsStore;
        _dotnet = dotnet;
        _os = os;

        SaveCommand = new RelayCommand(Save);
        RefreshDotNetInfoCommand = new AsyncRelayCommand(RefreshDotNetInfoAsync);
        OpenDotNet9DownloadCommand = new RelayCommand(() => _os.OpenUrl("https://dotnet.microsoft.com/download/dotnet/9.0"));
        OpenDotNet10DownloadCommand = new RelayCommand(() => _os.OpenUrl("https://dotnet.microsoft.com/download/dotnet/10.0"));

        Load();
        _ = RefreshDotNetInfoAsync();
    }

    [ObservableProperty]
    private string _projectsRoot = string.Empty;

    [ObservableProperty]
    private string _nugetPackagesFolder = string.Empty;

    [ObservableProperty]
    private string _preferredIde = "VSCode";

    [ObservableProperty]
    private ProjectsOrderingOption _selectedProjectsOrdering = null!;

    [ObservableProperty]
    private string _dotNetVersion = string.Empty;

    [ObservableProperty]
    private bool _skipSplashScreen;

    public string[] IdeOptions { get; } = ["VSCode", "Rider", "Folder"]; 

    public sealed record ProjectsOrderingOption(string DisplayName, string Value)
    {
        public override string ToString() => DisplayName;
    }

    public ProjectsOrderingOption[] ProjectsOrderingOptions { get; } =
    [
        new ProjectsOrderingOption("Last Used (managed by Hub)", "LastUsed"),
        new ProjectsOrderingOption("Project Last Updated (file timestamp)", "ProjectUpdated")
    ];

    public IRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand RefreshDotNetInfoCommand { get; }
    public IRelayCommand OpenDotNet9DownloadCommand { get; }
    public IRelayCommand OpenDotNet10DownloadCommand { get; }

    private void Load()
    {
        var settings = _settingsStore.Load();
        ProjectsRoot = settings.ProjectsRoot ?? string.Empty;
        NugetPackagesFolder = settings.NuGetPackagesFolder ?? string.Empty;
        PreferredIde = settings.PreferredIde;
        SkipSplashScreen = settings.SkipSplashScreen;

        var mode = string.IsNullOrWhiteSpace(settings.ProjectsOrderingMode) ? "ProjectUpdated" : settings.ProjectsOrderingMode;
        SelectedProjectsOrdering = ProjectsOrderingOptions.FirstOrDefault(o => o.Value.Equals(mode, StringComparison.OrdinalIgnoreCase))
            ?? ProjectsOrderingOptions[1];
    }

    private void Save()
    {
        var current = _settingsStore.Load();
        var updated = current with
        {
            ProjectsRoot = string.IsNullOrWhiteSpace(ProjectsRoot) ? null : ProjectsRoot,
            NuGetPackagesFolder = string.IsNullOrWhiteSpace(NugetPackagesFolder) ? null : NugetPackagesFolder,
            PreferredIde = PreferredIde,
            ProjectsOrderingMode = SelectedProjectsOrdering.Value,
            SkipSplashScreen = SkipSplashScreen
        };

        _settingsStore.Save(updated);
        Log("Settings saved.");

        if (!string.IsNullOrWhiteSpace(updated.NuGetPackagesFolder))
        {
            try
            {
                Environment.SetEnvironmentVariable("NUGET_PACKAGES", updated.NuGetPackagesFolder, EnvironmentVariableTarget.User);
                Log("Set user environment variable NUGET_PACKAGES (Windows). New shells will pick it up.");
            }
            catch
            {
                Log("NuGet cache folder is applied to Hub-invoked dotnet processes via NUGET_PACKAGES.");
                Log("To persist it system-wide on macOS/Linux, update your shell profile (e.g., ~/.zshrc, ~/.bashrc)." );
            }
        }
    }

    partial void OnSkipSplashScreenChanged(bool value)
    {
        var current = _settingsStore.Load();
        if (current.SkipSplashScreen == value)
            return;

        _settingsStore.Save(current with { SkipSplashScreen = value });
        Log(value ? "Splashscreen will be skipped on startup." : "Splashscreen will be shown on startup.");
    }

    private async Task RefreshDotNetInfoAsync()
    {
        var lines = new List<string>();
        var progress = new Progress<string>(line => lines.Add(line));

        try
        {
            var exit = await _dotnet.RunAsync(new[] { "--version" }, null, null, progress, CancellationToken.None);
            DotNetVersion = exit == 0 ? (lines.LastOrDefault() ?? string.Empty) : "(unknown)";
        }
        catch
        {
            DotNetVersion = "(unknown)";
        }
    }
}
