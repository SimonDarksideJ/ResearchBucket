using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class InstallsViewModel : LoggableViewModel
{
    private readonly SettingsStore _settingsStore;
    private readonly TemplateManager _templates;
    private readonly NuGetVersionResolver _nuget;
    private readonly DotNetCli _dotnet;
    private readonly TemplatePackState _templateState;

    private CancellationTokenSource? _templatesCts;

    public InstallsViewModel(SettingsStore settingsStore, TemplateManager templates, NuGetVersionResolver nuget, DotNetCli dotnet, TemplatePackState templateState)
    {
        _settingsStore = settingsStore;
        _templates = templates;
        _nuget = nuget;
        _dotnet = dotnet;
        _templateState = templateState;

        RefreshAvailableVersionsCommand = new AsyncRelayCommand(RefreshAvailableVersionsAsync);
        InstallSelectedVersionCommand = new AsyncRelayCommand(InstallSelectedAsync);

        TemplatePackageId = _templateState.TemplatePackageId;

        _templateState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TemplatePackState.InstalledTemplatePackVersion))
            {
                OnPropertyChanged(nameof(InstalledTemplatePackVersion));
                UpdateInstallState();
            }
        };

        // Auto-sync on startup (best-effort).
        _ = RefreshAllAsync();

        TemplatesInSelectedVersion.CollectionChanged += OnTemplatesCollectionChanged;
    }

    public async Task OnNavigatedToAsync()
    {
        _templateState.ReloadFromSettings();
        TemplatePackageId = _templateState.TemplatePackageId;
        await RefreshInstalledVersionAsync();
    }

    [ObservableProperty]
    private string _templatePackageId = "MonoGame.Templates.CSharp";

    public string InstalledTemplatePackVersion => _templateState.InstalledTemplatePackVersion;

    public ObservableCollection<TemplateVersionInfo> AvailableVersions { get; } = new();

    // Default: show only latest stable + latest prerelease.
    [ObservableProperty]
    private bool _showAllVersions;

    [ObservableProperty]
    private TemplateVersionInfo? _selectedVersion;

    public ObservableCollection<TemplatePackTemplateInfo> TemplatesInSelectedVersion { get; } = new();

    [ObservableProperty]
    private bool _isLoadingTemplates;

    public bool HasSelectedVersion => SelectedVersion is not null;
    public bool NoSelectedVersion => SelectedVersion is null;
    public bool HasTemplates => TemplatesInSelectedVersion.Count > 0;
    public bool NoTemplates => TemplatesInSelectedVersion.Count == 0;

    public bool ShowSelectVersionHint => NoSelectedVersion;
    public bool ShowLoadingTemplatesHint => HasSelectedVersion && IsLoadingTemplates;
    public bool ShowNoTemplatesHint => HasSelectedVersion && !IsLoadingTemplates && NoTemplates;

    [ObservableProperty]
    private bool _isSelectedVersionInstalled;

    [ObservableProperty]
    private bool _canInstallSelectedVersion;

    [ObservableProperty]
    private bool _isLogExpanded;

    public IAsyncRelayCommand RefreshAvailableVersionsCommand { get; }
    public IAsyncRelayCommand InstallSelectedVersionCommand { get; }

    public bool ShowInstallButton => SelectedVersion is not null;

    partial void OnSelectedVersionChanged(TemplateVersionInfo? value)
    {
        UpdateInstallState();
        OnPropertyChanged(nameof(ShowInstallButton));

        OnPropertyChanged(nameof(HasSelectedVersion));
        OnPropertyChanged(nameof(NoSelectedVersion));
        OnPropertyChanged(nameof(ShowSelectVersionHint));
        OnPropertyChanged(nameof(ShowLoadingTemplatesHint));
        OnPropertyChanged(nameof(ShowNoTemplatesHint));

        // Refresh template list automatically for the selected version.
        if (value is null)
            return;

        // Avoid reload loops when the selection instance changes but the version doesn't.
        if (_lastLoadedTemplatesVersion is not null
            && _lastLoadedTemplatesVersion.Equals(value.Version, StringComparison.OrdinalIgnoreCase)
            && TemplatesInSelectedVersion.Count > 0
            && !IsLoadingTemplates)
        {
            return;
        }

        _ = RefreshTemplatesForSelectedVersionAsync();
    }

    partial void OnIsLoadingTemplatesChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowLoadingTemplatesHint));
        OnPropertyChanged(nameof(ShowNoTemplatesHint));
    }

    private void OnTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasTemplates));
        OnPropertyChanged(nameof(NoTemplates));
        OnPropertyChanged(nameof(ShowNoTemplatesHint));
    }

    partial void OnShowAllVersionsChanged(bool value)
    {
        // Re-apply filter to current list.
        ApplyAvailableVersionsFilter(keepSelectionIfPossible: true);
    }

    private async Task RefreshAllAsync()
    {
        await RefreshInstalledVersionAsync();
        await RefreshAvailableVersionsAsync();
    }

    private async Task RefreshAvailableVersionsAsync()
    {
        _templateState.SetTemplatePackageId(TemplatePackageId);

        try
        {
            var previouslySelected = SelectedVersion?.Version;

            Log($"Querying NuGet for versions: {TemplatePackageId}");
            var allVersions = await _templates.GetAvailableVersionsAsync(TemplatePackageId, _nuget, CancellationToken.None);

            // Keep an unfiltered copy for filtering.
            _allAvailableVersions = allVersions.ToList();

            ApplyAvailableVersionsFilter(keepSelectionIfPossible: true, previouslySelected);

            // Re-evaluate install state when versions change.
            UpdateInstallState();

            Log($"Found {_allAvailableVersions.Count} version(s) (showing {AvailableVersions.Count}).");
        }
        catch (Exception ex)
        {
            Log($"Version sync failed: {ex.Message}");
        }
    }

    private List<TemplateVersionInfo> _allAvailableVersions = new();

    private void ApplyAvailableVersionsFilter(bool keepSelectionIfPossible, string? previouslySelectedVersion = null)
    {
        var selectionKey = previouslySelectedVersion ?? SelectedVersion?.Version;

        // If we don't have any version data yet, don't clear the UI list.
        // (Startup can call this while a refresh is still in-flight.)
        if (_allAvailableVersions.Count == 0 && string.IsNullOrWhiteSpace(InstalledTemplatePackVersion))
            return;

        IReadOnlyList<TemplateVersionInfo> visible;
        if (ShowAllVersions)
        {
            visible = _allAvailableVersions;
        }
        else
        {
            var latestStable = _allAvailableVersions.FirstOrDefault(v => !v.IsPrerelease);
            var latestPrerelease = _allAvailableVersions.FirstOrDefault(v => v.IsPrerelease);

            visible = new[] { latestPrerelease, latestStable }
                .Where(v => v is not null)
                .Cast<TemplateVersionInfo>()
                .ToList();
        }

        // If NuGet versions aren't loaded yet, still show the installed version if known.
        if (visible.Count == 0 && !string.IsNullOrWhiteSpace(InstalledTemplatePackVersion))
        {
            visible = new[]
            {
                new TemplateVersionInfo(
                    Version: InstalledTemplatePackVersion,
                    IsPrerelease: InstalledTemplatePackVersion.Contains('-', StringComparison.Ordinal))
            };
        }

        // Always include the currently installed version so users can inspect its templates.
        if (!string.IsNullOrWhiteSpace(InstalledTemplatePackVersion)
            && !visible.Any(v => v.Version.Equals(InstalledTemplatePackVersion, StringComparison.OrdinalIgnoreCase)))
        {
            var installed = _allAvailableVersions.FirstOrDefault(v => v.Version.Equals(InstalledTemplatePackVersion, StringComparison.OrdinalIgnoreCase));
            if (installed is not null)
            {
                visible = visible.Concat(new[] { installed }).ToList();
            }
            else
            {
                // NuGet listing may not include locally installed versions (offline feeds, unlisted, etc).
                // Still show the installed version so the UI reflects what's on disk.
                visible = visible.Concat(new[]
                {
                    new TemplateVersionInfo(
                        Version: InstalledTemplatePackVersion,
                        IsPrerelease: InstalledTemplatePackVersion.Contains('-', StringComparison.Ordinal))
                }).ToList();
            }
        }

        if (visible.Count == 0)
            return;

        AvailableVersions.Clear();
        foreach (var v in visible)
            AvailableVersions.Add(v);

        if (!keepSelectionIfPossible)
            return;

        // Prefer selecting the installed version on startup.
        if (string.IsNullOrWhiteSpace(selectionKey) && !string.IsNullOrWhiteSpace(InstalledTemplatePackVersion))
            selectionKey = InstalledTemplatePackVersion;

        var candidate = string.IsNullOrWhiteSpace(selectionKey)
            ? AvailableVersions.FirstOrDefault()
            : (AvailableVersions.FirstOrDefault(v => v.Version.Equals(selectionKey, StringComparison.OrdinalIgnoreCase))
                ?? AvailableVersions.FirstOrDefault());

        if (candidate is null)
            return;

        // Only update SelectedVersion if the selected *version string* changes.
        if (SelectedVersion is null || !SelectedVersion.Version.Equals(candidate.Version, StringComparison.OrdinalIgnoreCase))
            SelectedVersion = candidate;
    }

    private async Task RefreshInstalledVersionAsync()
    {
        var progress = new Progress<string>(Log);

        try
        {
            _templateState.SetTemplatePackageId(TemplatePackageId);
            await _templateState.RefreshInstalledVersionAsync(progress, CancellationToken.None);
            OnPropertyChanged(nameof(InstalledTemplatePackVersion));
            UpdateInstallState();
        }
        catch (Exception ex)
        {
            Log($"Installed version check failed: {ex.Message}");
        }
    }

    private async Task RefreshTemplatesForSelectedVersionAsync()
    {
        IsLoadingTemplates = false;
        TemplatesInSelectedVersion.Clear();
        OnPropertyChanged(nameof(HasTemplates));
        OnPropertyChanged(nameof(NoTemplates));
        OnPropertyChanged(nameof(ShowNoTemplatesHint));

        if (SelectedVersion is null)
            return;

        _templatesCts?.Cancel();
        _templatesCts?.Dispose();
        _templatesCts = new CancellationTokenSource();
        var ct = _templatesCts.Token;

        var progress = new Progress<string>(Log);

        try
        {
            var settings = _settingsStore.Load();
            var selectedVersion = SelectedVersion.Version;
            Log($"Loading templates for {TemplatePackageId}::{selectedVersion} ...");

            IsLoadingTemplates = true;
            _lastLoadedTemplatesVersion = selectedVersion;
            var templates = await _templates.ListTemplatesInTemplatePackAsync(
                TemplatePackageId,
                selectedVersion,
                settings.NuGetPackagesFolder,
                progress,
                ct);

            foreach (var t in templates)
                TemplatesInSelectedVersion.Add(t);

            Log($"Loaded {TemplatesInSelectedVersion.Count} template(s) for {selectedVersion}.");
        }
        catch (OperationCanceledException)
        {
            // Ignore rapid selection changes.
        }
        catch (Exception ex)
        {
            Log($"Template list load failed: {ex.Message}");
        }
        finally
        {
            IsLoadingTemplates = false;
        }
    }

    private string? _lastLoadedTemplatesVersion;

    private void UpdateInstallState()
    {
        if (SelectedVersion is null)
        {
            IsSelectedVersionInstalled = false;
            CanInstallSelectedVersion = false;
            return;
        }

        IsSelectedVersionInstalled =
            !string.IsNullOrWhiteSpace(InstalledTemplatePackVersion)
            && SelectedVersion.Version.Equals(InstalledTemplatePackVersion, StringComparison.OrdinalIgnoreCase);

        CanInstallSelectedVersion = !IsSelectedVersionInstalled;
    }

    private async Task InstallSelectedAsync()
    {
        IsLogExpanded = true;

        if (SelectedVersion is null)
        {
            Log("Select a version first.");
            return;
        }

        if (IsSelectedVersionInstalled)
        {
            Log("Selected version is already installed.");
            return;
        }

        var settings = _settingsStore.Load();
        var progress = new Progress<string>(Log);

        _templateState.SetTemplatePackageId(TemplatePackageId);

        Log("Uninstalling existing templates (best-effort)...");
        await _templates.UninstallAsync(TemplatePackageId, settings.NuGetPackagesFolder, progress, CancellationToken.None);

        Log($"Installing {TemplatePackageId}::{SelectedVersion.Version} ...");
        var exit = await _templates.InstallAsync(TemplatePackageId, SelectedVersion.Version, settings.NuGetPackagesFolder, progress, CancellationToken.None);

        Log(exit == 0 ? "Install complete." : $"Install failed (exit {exit}).");

        await RefreshInstalledVersionAsync();

        // Re-load templates from the selected version (should match installed).
        await RefreshTemplatesForSelectedVersionAsync();
    }
}
