using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class NewProjectViewModel : LoggableViewModel
{
    private readonly SettingsStore _settingsStore;
    private readonly DotNetCli _dotnet;
    private readonly TemplateManager _templates;
    private readonly MonoGameToolingVersionSync _toolingVersionSync;
    private readonly OsLauncher _os;
    private readonly TemplatePackState _templateState;

    public NewProjectViewModel(SettingsStore settingsStore, DotNetCli dotnet, TemplateManager templates, MonoGameToolingVersionSync toolingVersionSync, OsLauncher os, TemplatePackState templateState)
    {
        _settingsStore = settingsStore;
        _dotnet = dotnet;
        _templates = templates;
        _toolingVersionSync = toolingVersionSync;
        _os = os;
        _templateState = templateState;

        CreateProjectCommand = new AsyncRelayCommand(CreateProjectAsync, CanCreateProject);
        RefreshTemplatesCommand = new AsyncRelayCommand(RefreshTemplatesAsync);

        var settings = _settingsStore.Load();
        OutputRoot = settings.ProjectsRoot ?? string.Empty;

        _templateState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TemplatePackState.InstalledTemplatePackVersion))
            {
                OnPropertyChanged(nameof(InstalledTemplatePackVersion));
                _ = RefreshTemplatesAsync();
            }
            else if (e.PropertyName == nameof(TemplatePackState.TemplatePackageId))
            {
                _ = RefreshTemplatesAsync();
            }
        };

        // Best-effort load options.
        _ = RefreshAllAsync();

        // Initial validation.
        RefreshProjectExistsState();
    }

    public async Task OnNavigatedToAsync()
    {
        // Refresh installed version when returning from Installs tab.
        _templateState.ReloadFromSettings();
        await RefreshInstalledTemplatePackVersionAsync();
    }

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _templateShortName = "mgdesktopgl";

    public ObservableCollection<TemplateInfo> TemplateOptions { get; } = new();

    [ObservableProperty]
    private TemplateInfo? _selectedTemplate;

    [ObservableProperty]
    private string _outputRoot = string.Empty;

    public string InstalledTemplatePackVersion => _templateState.InstalledTemplatePackVersion;

    [ObservableProperty]
    private bool _isLogExpanded;

    [ObservableProperty]
    private bool _projectFolderAlreadyExists;

    [ObservableProperty]
    private bool _projectsRootMissingWarning;

    [ObservableProperty]
    private bool _isTemplatesLoading;

    partial void OnProjectNameChanged(string value)
    {
        RefreshProjectExistsState();
    }

    partial void OnOutputRootChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            ProjectsRootMissingWarning = false;

        RefreshProjectExistsState();
    }

    public void SetAndPersistProjectsRoot(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return;

        OutputRoot = folder;
        ProjectsRootMissingWarning = false;

        var current = _settingsStore.Load();
        if (!string.Equals(current.ProjectsRoot ?? string.Empty, folder, StringComparison.Ordinal))
            _settingsStore.Save(current with { ProjectsRoot = folder });
    }

    private void RefreshProjectExistsState()
    {
        ProjectFolderAlreadyExists = false;

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            CreateProjectCommand.NotifyCanExecuteChanged();
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputRoot) || !Directory.Exists(OutputRoot))
        {
            CreateProjectCommand.NotifyCanExecuteChanged();
            return;
        }

        var projectFolder = Path.Combine(OutputRoot, ProjectName);
        ProjectFolderAlreadyExists = Directory.Exists(projectFolder);
        CreateProjectCommand.NotifyCanExecuteChanged();
    }

    private bool CanCreateProject()
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
            return false;

        // Allow the user to click Create even when no output root is set yet;
        // the view will prompt for a folder and persist it.
        if (string.IsNullOrWhiteSpace(OutputRoot))
            return true;

        if (!Directory.Exists(OutputRoot))
            return false;

        return !ProjectFolderAlreadyExists;
    }

    public IAsyncRelayCommand CreateProjectCommand { get; }
    public IAsyncRelayCommand RefreshTemplatesCommand { get; }

    private async Task RefreshAllAsync()
    {
        await RefreshInstalledTemplatePackVersionAsync();
        await RefreshTemplatesAsync();
    }

    private async Task RefreshInstalledTemplatePackVersionAsync()
    {
        var progress = new Progress<string>(Log);

        try
        {
            await _templateState.RefreshInstalledVersionAsync(progress, CancellationToken.None);
            OnPropertyChanged(nameof(InstalledTemplatePackVersion));
        }
        catch (Exception ex)
        {
            Log($"Installed version check failed: {ex.Message}");
        }
    }

    private async Task RefreshTemplatesAsync()
    {
        // Prevent overlapping refreshes (e.g. user clicks Refresh while a version-change refresh is already running).
        if (Interlocked.Exchange(ref _refreshTemplatesInFlight, 1) == 1)
            return;

        IsTemplatesLoading = true;
        TemplateOptions.Clear();

        // Ensure we don't keep a SelectedTemplate reference that no longer exists
        // once we refresh the template list (e.g., when the installed template pack version changes).
        SelectedTemplate = null;

        var settings = _settingsStore.Load();
        var progress = new Progress<string>(Log);

        try
        {
            Log("Loading templates from dotnet...");
            var templates = await _templates.ListMonoGameTemplateOptionsAsync(
                settings.NuGetPackagesFolder,
                progress,
                CancellationToken.None);

            foreach (var t in templates)
                TemplateOptions.Add(t);

            // Re-select from the refreshed list using the current TemplateShortName.
            SelectedTemplate = TemplateOptions.FirstOrDefault(t => t.TemplateId.Equals(TemplateShortName, StringComparison.OrdinalIgnoreCase))
                ?? TemplateOptions.FirstOrDefault();

            if (SelectedTemplate is not null)
                TemplateShortName = SelectedTemplate.TemplateId;

            Log($"Loaded {TemplateOptions.Count} template(s).");
        }
        catch (Exception ex)
        {
            Log($"Template load failed: {ex.Message}");
        }
        finally
        {
            IsTemplatesLoading = false;
            Interlocked.Exchange(ref _refreshTemplatesInFlight, 0);
        }
    }

    private int _refreshTemplatesInFlight;

    partial void OnSelectedTemplateChanged(TemplateInfo? value)
    {
        if (value is not null)
            TemplateShortName = value.TemplateId;
    }

    private async Task CreateProjectAsync()
    {
        IsLogExpanded = true;

        if (string.IsNullOrWhiteSpace(OutputRoot))
            ProjectsRootMissingWarning = true;

        RefreshProjectExistsState();
        if (ProjectFolderAlreadyExists)
        {
            Log("A project already exists with this name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            Log("Project name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputRoot) || !Directory.Exists(OutputRoot))
        {
            Log("Select a valid output root folder.");
            return;
        }

        var settings = _settingsStore.Load();
        var projectFolder = Path.Combine(OutputRoot, ProjectName);

        Directory.CreateDirectory(projectFolder);

        var env = string.IsNullOrWhiteSpace(settings.NuGetPackagesFolder)
            ? null
            : new Dictionary<string, string?> { ["NUGET_PACKAGES"] = settings.NuGetPackagesFolder };

        var progress = new Progress<string>(Log);

        Log($"dotnet new {TemplateShortName} -n {ProjectName} -o {projectFolder}");
        var exitNew = await _dotnet.RunAsync(
            new[] { "new", TemplateShortName, "-n", ProjectName, "-o", projectFolder },
            workingDirectory: OutputRoot,
            environment: env,
            output: progress,
            cancellationToken: CancellationToken.None);

        if (exitNew != 0)
        {
            Log($"dotnet new failed (exit {exitNew}).");
            return;
        }

        // Ensure MonoGame package versions in generated csproj(s) match the required dotnet tool version
        // BEFORE running `dotnet tool restore` and `dotnet restore`.
        try
        {
            var didSync = _toolingVersionSync.TrySyncMonoGameVersionsToDotNetTools(projectFolder, Log);
            if (didSync)
                Log("Updated MonoGame package versions to match dotnet-tools.");
        }
        catch (Exception ex)
        {
            Log($"MonoGame version sync failed: {ex.Message}");
        }

        // If the template uses dotnet tools (common for older tooling setups), restore them first.
        var toolsManifest = Path.Combine(projectFolder, ".config", "dotnet-tools.json");
        if (File.Exists(toolsManifest))
        {
            Log("Running dotnet tool restore...");
            var exitToolRestore = await _dotnet.RunAsync(
                new[] { "tool", "restore" },
                workingDirectory: projectFolder,
                environment: env,
                output: progress,
                cancellationToken: CancellationToken.None);

            if (exitToolRestore != 0)
            {
                Log($"dotnet tool restore failed (exit {exitToolRestore}).");
                return;
            }
        }

        Log("Running dotnet restore...");
        var exitRestore = await _dotnet.RunAsync(
            new[] { "restore" },
            workingDirectory: projectFolder,
            environment: env,
            output: progress,
            cancellationToken: CancellationToken.None);

        if (exitRestore != 0)
        {
            Log($"dotnet restore failed (exit {exitRestore}).");
            return;
        }

        Log("Project created and restored.");

        // Best-effort open.
        try
        {
            var ide = settings.PreferredIde;
            if (ide.Equals("VSCode", StringComparison.OrdinalIgnoreCase))
                _os.OpenInVsCode(projectFolder);
            else if (ide.Equals("Rider", StringComparison.OrdinalIgnoreCase))
                _os.OpenInRider(projectFolder);
            else
                _os.OpenFolder(projectFolder);
        }
        catch (Exception ex)
        {
            Log($"Open failed: {ex.Message}");
        }
    }
}
