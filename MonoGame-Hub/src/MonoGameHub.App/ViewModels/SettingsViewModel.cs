using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MonoGameHub.App.ViewModels;

public sealed partial class SettingsViewModel : LoggableViewModel
{
    private readonly SettingsStore _settingsStore;
    private readonly DotNetCli _dotnet;
    private readonly OsLauncher _os;
    private readonly DotNetWorkloadState _workloads;
    private readonly DotNetWorkloadManager _workloadManager;

    public SettingsViewModel(
        SettingsStore settingsStore,
        DotNetCli dotnet,
        OsLauncher os,
        DotNetWorkloadState workloads,
        DotNetWorkloadManager workloadManager)
    {
        _settingsStore = settingsStore;
        _dotnet = dotnet;
        _os = os;
        _workloads = workloads;
        _workloadManager = workloadManager;

        SaveCommand = new RelayCommand(Save);
        RefreshDotNetInfoCommand = new AsyncRelayCommand(RefreshDotNetInfoAsync);
        OpenDotNet9DownloadCommand = new RelayCommand(() => _os.OpenUrl("https://dotnet.microsoft.com/download/dotnet/9.0"));
        OpenDotNet10DownloadCommand = new RelayCommand(() => _os.OpenUrl("https://dotnet.microsoft.com/download/dotnet/10.0"));

        Load();
        _ = RefreshDotNetInfoAsync();

        _workloads.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DotNetWorkloadState.AvailableWorkloads))
            {
                RefreshWorkloadChoicesFromState();
            }
            else if (e.PropertyName == nameof(DotNetWorkloadState.InstalledWorkloads))
            {
                RefreshWorkloadChoicesFromState();
            }
        };

        RefreshWorkloadChoicesFromState();
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

    [ObservableProperty]
    private bool _isLogExpanded;

    public ObservableCollection<WorkloadChoiceItem> WorkloadChoices { get; } = new();

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

        _desiredWorkloads = (settings.DesiredWorkloads ?? Array.Empty<string>())
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ApplyDesiredWorkloads(_desiredWorkloads);
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
            SkipSplashScreen = SkipSplashScreen,
            DesiredWorkloads = _desiredWorkloads
                .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                .ToArray()
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

    private void RefreshWorkloadChoicesFromState()
    {
        // Render installed workloads as checked, plus any user-desired workloads.
        var installed = _workloads.InstalledWorkloads
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var available = _workloads.AvailableWorkloads;
        if (available.Count == 0)
            return;

        _suppressWorkloadSelectionHandling = true;
        try
        {
            WorkloadChoices.Clear();
            foreach (var id in available)
            {
                var isInstalled = installed.Contains(id);
                var shouldCheck = isInstalled || _desiredWorkloads.Contains(id);
                var item = new WorkloadChoiceItem(id, isInstalled, shouldCheck);
                item.SelectionChanged += OnWorkloadChoiceSelectionChanged;
                WorkloadChoices.Add(item);
            }

            // Also include any desired workloads that aren't discoverable in the list.
            foreach (var missing in _desiredWorkloads.Where(s => !available.Contains(s, StringComparer.OrdinalIgnoreCase)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                var item = new WorkloadChoiceItem(missing, isInstalled: false, isSelected: true);
                item.SelectionChanged += OnWorkloadChoiceSelectionChanged;
                WorkloadChoices.Add(item);
            }
        }
        finally
        {
            _suppressWorkloadSelectionHandling = false;
        }
    }

    private void ApplyDesiredWorkloads(IReadOnlySet<string> desired)
    {
        // Desired workloads are persisted and will be merged with installed workloads when
        // the installed list is available. If the UI list isn't ready yet, just add the desired entries.
        if (WorkloadChoices.Count == 0 && desired.Count > 0)
        {
            _suppressWorkloadSelectionHandling = true;
            try
            {
                foreach (var id in desired.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                {
                    var item = new WorkloadChoiceItem(id, isInstalled: false, isSelected: true);
                    item.SelectionChanged += OnWorkloadChoiceSelectionChanged;
                    WorkloadChoices.Add(item);
                }
            }
            finally
            {
                _suppressWorkloadSelectionHandling = false;
            }
        }
    }

    private void OnWorkloadChoiceSelectionChanged(WorkloadChoiceItem item, bool isSelected)
    {
        if (_suppressWorkloadSelectionHandling)
            return;

        // Installed workloads remain installed; we never uninstall.
        // If user tries to uncheck an installed workload, revert the checkbox back on.
        if (!isSelected && item.IsInstalled)
        {
            _suppressWorkloadSelectionHandling = true;
            try
            {
                item.IsSelected = true;
            }
            finally
            {
                _suppressWorkloadSelectionHandling = false;
            }

            return;
        }

        if (isSelected)
            _desiredWorkloads.Add(item.WorkloadId);
        else
            _desiredWorkloads.Remove(item.WorkloadId);

        PersistDesiredWorkloads();

        if (!isSelected)
            return;

        // Only act when user checks a workload that is not currently installed.
        if (_workloads.IsWorkloadInstalled(item.WorkloadId))
            return;

        _ = InstallWorkloadFromSelectionAsync(item.WorkloadId);
    }

    private void PersistDesiredWorkloads()
    {
        var current = _settingsStore.Load();
        var updated = current with
        {
            DesiredWorkloads = _desiredWorkloads
                .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };

        _settingsStore.Save(updated);
    }

    private async Task InstallWorkloadFromSelectionAsync(string workloadId)
    {
        if (string.IsNullOrWhiteSpace(workloadId))
            return;

        IsLogExpanded = true;

        var progress = new Progress<string>(Log);
        try
        {
            Log($"Installing workload '{workloadId}'...");
            var exit = await _workloadManager.InstallAsync(workloadId, progress, CancellationToken.None);
            Log(exit == 0 ? $"Workload '{workloadId}' installed." : $"Workload '{workloadId}' install failed (exit {exit}).");

            await _workloads.RefreshAsync(progress, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log($"Workload install failed: {ex.Message}");
        }
    }

    public sealed partial class WorkloadChoiceItem : ObservableObject
    {
        public WorkloadChoiceItem(string workloadId, bool isInstalled, bool isSelected)
        {
            WorkloadId = workloadId;
            IsInstalled = isInstalled;
            _isSelected = isSelected;
        }

        public string WorkloadId { get; }

        public bool IsInstalled { get; }

        public event Action<WorkloadChoiceItem, bool>? SelectionChanged;

        [ObservableProperty]
        private bool _isSelected;

        partial void OnIsSelectedChanged(bool value)
        {
            SelectionChanged?.Invoke(this, value);
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

    private bool _suppressWorkloadSelectionHandling;

    private HashSet<string> _desiredWorkloads = new(StringComparer.OrdinalIgnoreCase);
}
