using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;
using NuGet.Versioning;

namespace MonoGameHub.App.ViewModels;

public sealed partial class ProjectsViewModel : LoggableViewModel
{
    private readonly SettingsStore _settingsStore;
    private readonly ProjectScanner _scanner;
    private readonly NuGetVersionResolver _nuget;
    private readonly OsLauncher _os;

    private bool _pendingRescan;
    private string _lastScannedRoot = string.Empty;

    private const string OrderingLastUsed = "LastUsed";
    private const string OrderingProjectUpdated = "ProjectUpdated";

    public sealed record ProjectListItem(
        ProjectInfo Project,
        string FolderDisplay,
        string VersionDisplay,
        string? DotNetToolsVersion,
        bool IsToolsMisconfigured,
        string ToolsMisconfigurationNote,
        IBrush? RowBackground,
        string LastActivityDisplay);

    public ProjectsViewModel(SettingsStore settingsStore, ProjectScanner scanner, NuGetVersionResolver nuget, OsLauncher os)
    {
        _settingsStore = settingsStore;
        _scanner = scanner;
        _nuget = nuget;
        _os = os;

        var settings = _settingsStore.Load();
        ProjectsRoot = settings.ProjectsRoot ?? string.Empty;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        SortOptions =
        [
            new ProjectSortOption("Date (Newest first)", ProjectSortMode.DateDesc),
            new ProjectSortOption("Date (Oldest first)", ProjectSortMode.DateAsc),
            new ProjectSortOption("Name (A → Z)", ProjectSortMode.NameAsc)
        ];

        SelectedSortOption = SortOptions[0];

        _settingsStore.SettingsChanged += OnSettingsChanged;
    }

    [ObservableProperty]
    private string _projectsRoot = string.Empty;

    public ObservableCollection<ProjectListItem> Projects { get; } = new();

    [ObservableProperty]
    private ProjectListItem? _selectedProjectItem;

    public ProjectInfo? SelectedProject => SelectedProjectItem?.Project;

    public bool HasSelectedProject => SelectedProject is not null;
    public bool NoSelectedProject => SelectedProject is null;

    public string LastActivityHeader => GetOrderingMode().Equals(OrderingLastUsed, StringComparison.OrdinalIgnoreCase)
        ? "Last Opened"
        : "Last Updated";

    partial void OnSelectedProjectItemChanged(ProjectListItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedProject));
        OnPropertyChanged(nameof(NoSelectedProject));
        OnPropertyChanged(nameof(SelectedProject));

        OpenProjectCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSortOptionChanged(ProjectSortOption value)
    {
        ApplySort(preserveSelection: true);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task OnNavigatedToAsync()
    {
        // If settings changed while user was away from the Projects tab, update + rescan on return.
        if (_pendingRescan && !string.IsNullOrWhiteSpace(ProjectsRoot))
        {
            _pendingRescan = false;
            await RefreshAsync();
            return;
        }

        // Also rescan if the root was changed externally.
        if (!string.Equals(_lastScannedRoot, ProjectsRoot, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(ProjectsRoot))
        {
            await RefreshAsync();
        }
    }

    private void OnSettingsChanged(object? sender, HubSettings settings)
    {
        var newRoot = settings.ProjectsRoot ?? string.Empty;

        if (!string.Equals(newRoot, ProjectsRoot, StringComparison.OrdinalIgnoreCase))
        {
            ProjectsRoot = newRoot;
            _pendingRescan = true;
        }

        // Ordering mode affects header + displayed date + sort.
        OnPropertyChanged(nameof(LastActivityHeader));
        ApplySort(preserveSelection: true);
    }

    private async Task RefreshAsync()
    {
        Projects.Clear();
        _allProjects.Clear();

        if (string.IsNullOrWhiteSpace(ProjectsRoot) || !Directory.Exists(ProjectsRoot))
        {
            Log("Select a valid projects folder.");
            return;
        }

        Log($"Scanning: {ProjectsRoot}");

        var progress = new Progress<string>(Log);

        using var cts = new CancellationTokenSource();

        IReadOnlyList<ProjectInfo> scanned;
        try
        {
            scanned = await _scanner.ScanAsync(
                ProjectsRoot,
                resolveVersion: async project =>
                {
                    // Resolve wildcard MonoGame versions only (best-effort).
                    if (project.MonoGameVersionSpec is null)
                        return null;

                    if (!project.MonoGameVersionSpec.Contains('*'))
                        return project.MonoGameVersionSpec;

                    // Resolve against a representative MonoGame package.
                    return await _nuget.ResolveWildcardAsync(
                        packageId: "MonoGame.Framework.DesktopGL",
                        wildcardVersionSpec: project.MonoGameVersionSpec,
                        cancellationToken: cts.Token);
                },
                cancellationToken: cts.Token);
        }
        catch (Exception ex)
        {
            Log($"Scan failed: {ex.Message}");
            return;
        }

        var loadedSettings = _settingsStore.Load();
        var mru = BuildMruMap(loadedSettings);
        foreach (var p in scanned)
        {
            mru.TryGetValue(p.ProjectPath, out var lastOpened);

            // If we now collapse multiple projects under a solution, older MRU entries may still
            // be stored against individual csproj paths. Best-effort: infer the solution's last
            // opened time from any recent entries under the solution folder.
            if (lastOpened is null && p.ProjectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                lastOpened = TryInferSolutionLastOpenedUtc(loadedSettings, p.ProjectPath);

            _allProjects.Add(p with { LastOpenedUtc = lastOpened });
        }

        ApplySort(preserveSelection: false);

        Log($"Found {Projects.Count} MonoGame-related project(s).");

        _lastScannedRoot = ProjectsRoot;

        // Persist root.
        var settings = loadedSettings with { ProjectsRoot = ProjectsRoot };
        _settingsStore.Save(settings);
    }

    private static DateTimeOffset? TryInferSolutionLastOpenedUtc(HubSettings settings, string solutionPath)
    {
        if (settings.RecentProjects is null || settings.RecentProjects.Count == 0)
            return null;

        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
            return null;

        var prefix = solutionDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        DateTimeOffset? best = null;

        foreach (var e in settings.RecentProjects)
        {
            if (string.IsNullOrWhiteSpace(e.ProjectPath))
                continue;

            if (!e.ProjectPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (best is null || e.LastOpenedUtc > best.Value)
                best = e.LastOpenedUtc;
        }

        return best;
    }

    private readonly List<ProjectInfo> _allProjects = new();

    public ObservableCollection<ProjectSortOption> SortOptions { get; }

    [ObservableProperty]
    private ProjectSortOption _selectedSortOption;

    public sealed record ProjectSortOption(string DisplayName, ProjectSortMode Mode)
    {
        public override string ToString() => DisplayName;
    }

    public enum ProjectSortMode
    {
        DateDesc,
        DateAsc,
        NameAsc
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProject))]
    private void OpenProject(ProjectInfo? project)
    {
        if (project is null)
            return;

        var projectFolder = Path.GetDirectoryName(project.ProjectPath);
        if (string.IsNullOrWhiteSpace(projectFolder) || !Directory.Exists(projectFolder))
        {
            Log("Project folder not found.");
            return;
        }

        var settings = _settingsStore.Load();
        var ide = settings.PreferredIde;

        // Always update MRU list (even if ordering uses file time).
        try
        {
            var now = DateTimeOffset.UtcNow;
            var updated = UpsertRecentProject(settings, project.ProjectPath, now);
            _settingsStore.Save(updated);

            // Update in-memory list and refresh list presentation.
            for (var i = 0; i < _allProjects.Count; i++)
            {
                if (_allProjects[i].ProjectPath.Equals(project.ProjectPath, StringComparison.OrdinalIgnoreCase))
                {
                    _allProjects[i] = _allProjects[i] with { LastOpenedUtc = now };
                    break;
                }
            }

            ApplySort(preserveSelection: true);
        }
        catch
        {
            // Best-effort MRU; ignore persistence failures.
        }

        try
        {
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

    private void ApplySort(bool preserveSelection)
    {
        if (SelectedSortOption is null)
            return;

        var selectedPath = preserveSelection ? SelectedProject?.ProjectPath : null;

        var orderingMode = GetOrderingMode();
        IEnumerable<ProjectInfo> ordered = _allProjects;

        switch (SelectedSortOption.Mode)
        {
            case ProjectSortMode.NameAsc:
                ordered = ordered.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                break;
            case ProjectSortMode.DateAsc:
                if (orderingMode.Equals(OrderingLastUsed, StringComparison.OrdinalIgnoreCase))
                {
                    // MRU: projects without last-opened timestamp come last, and are then ordered by name.
                    ordered = ordered
                        .OrderBy(p => p.LastOpenedUtc is null)
                        .ThenBy(p => p.LastOpenedUtc)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    ordered = ordered
                        .OrderBy(p => p.ProjectFileLastWriteTimeUtc)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                break;
            case ProjectSortMode.DateDesc:
            default:
                if (orderingMode.Equals(OrderingLastUsed, StringComparison.OrdinalIgnoreCase))
                {
                    // MRU requirement: last opened first, everything else afterwards by name.
                    ordered = ordered
                        .OrderBy(p => p.LastOpenedUtc is null)
                        .ThenByDescending(p => p.LastOpenedUtc)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    ordered = ordered
                        .OrderByDescending(p => p.ProjectFileLastWriteTimeUtc)
                        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                break;
        }

        // Final UI-layer safety net: never render the same path twice.
        var uniqueOrdered = ordered
            .GroupBy(p => Path.GetFullPath(p.ProjectPath), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());

        Projects.Clear();
        foreach (var p in uniqueOrdered)
        {
            var display = BuildLastActivityDisplay(p, orderingMode);

            var folder = Path.GetDirectoryName(p.ProjectPath) ?? string.Empty;
            var version = !string.IsNullOrWhiteSpace(p.ResolvedMonoGameVersion)
                ? p.ResolvedMonoGameVersion!
                : (p.MonoGameVersionSpec ?? string.Empty);

            var toolsVersion = TryGetDotNetToolsVersion(folder);

            var (isMisconfigured, note, background) = BuildToolsMisconfiguration(p, toolsVersion);

            Projects.Add(new ProjectListItem(
                Project: p,
                FolderDisplay: folder,
                VersionDisplay: version,
                DotNetToolsVersion: toolsVersion,
                IsToolsMisconfigured: isMisconfigured,
                ToolsMisconfigurationNote: note,
                RowBackground: background,
                LastActivityDisplay: display));
        }

        if (!string.IsNullOrWhiteSpace(selectedPath))
            SelectedProjectItem = Projects.FirstOrDefault(p => p.Project.ProjectPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase));
    }

    private string GetOrderingMode()
    {
        var settings = _settingsStore.Load();
        return string.IsNullOrWhiteSpace(settings.ProjectsOrderingMode)
            ? OrderingProjectUpdated
            : settings.ProjectsOrderingMode;
    }

    private static Dictionary<string, DateTimeOffset?> BuildMruMap(HubSettings settings)
    {
        var map = new Dictionary<string, DateTimeOffset?>(StringComparer.OrdinalIgnoreCase);

        if (settings.RecentProjects is null)
            return map;

        foreach (var e in settings.RecentProjects)
        {
            if (string.IsNullOrWhiteSpace(e.ProjectPath))
                continue;

            map[e.ProjectPath] = e.LastOpenedUtc;
        }

        return map;
    }

    private static HubSettings UpsertRecentProject(HubSettings settings, string projectPath, DateTimeOffset openedUtc)
    {
        var list = (settings.RecentProjects ?? Array.Empty<RecentProjectEntry>()).ToList();
        var idx = list.FindIndex(p => p.ProjectPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase));

        var entry = new RecentProjectEntry(projectPath, openedUtc);
        if (idx >= 0)
            list[idx] = entry;
        else
            list.Add(entry);

        // Keep list bounded.
        list = list
            .OrderByDescending(p => p.LastOpenedUtc)
            .Take(100)
            .ToList();

        return settings with { RecentProjects = list };
    }

    private static string BuildLastActivityDisplay(ProjectInfo project, string orderingMode)
    {
        DateTimeOffset? when = orderingMode.Equals(OrderingLastUsed, StringComparison.OrdinalIgnoreCase)
            ? project.LastOpenedUtc
            : project.ProjectFileLastWriteTimeUtc;

        if (when is null)
            return string.Empty;

        // Local display.
        return when.Value.ToLocalTime().ToString("g");
    }

    private static string? TryGetDotNetToolsVersion(string projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !Directory.Exists(projectFolder))
            return null;

        var toolsPath = Path.Combine(projectFolder, ".config", "dotnet-tools.json");
        if (!File.Exists(toolsPath))
            return null;

        try
        {
            using var stream = File.OpenRead(toolsPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("tools", out var toolsElement) || toolsElement.ValueKind != JsonValueKind.Object)
                return null;

            // Prefer the MonoGame tool if present.
            if (toolsElement.TryGetProperty("dotnet-mgcb", out var mgcb) && mgcb.ValueKind == JsonValueKind.Object)
            {
                if (mgcb.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }

            // Otherwise use the first tool version.
            foreach (var tool in toolsElement.EnumerateObject())
            {
                if (tool.Value.ValueKind != JsonValueKind.Object)
                    continue;

                if (tool.Value.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
        }
        catch
        {
            // Best-effort only.
        }

        return null;
    }

    private static (bool IsMisconfigured, string Note, IBrush? Background) BuildToolsMisconfiguration(ProjectInfo project, string? dotnetToolsVersion)
    {
        if (string.IsNullOrWhiteSpace(dotnetToolsVersion))
            return (false, string.Empty, null);

        var projectSpec = project.MonoGameVersionSpec ?? string.Empty;
        var resolved = project.ResolvedMonoGameVersion;
        var projectEffective = resolved ?? projectSpec;

        if (string.IsNullOrWhiteSpace(projectEffective))
            return (false, string.Empty, null);

        var mismatch = !VersionsMatch(dotnetToolsVersion, projectEffective);
        if (!mismatch)
            return (false, string.Empty, null);

        var resolvedDisplay = string.IsNullOrWhiteSpace(resolved) ? "" : $" (resolves to {resolved})";

        var note =
            "DotNet tools misconfiguration:\n" +
            $"- dotnet-tools version = {dotnetToolsVersion}\n" +
            $"- project version = {projectSpec}{resolvedDisplay}\n\n" +
            "Please update the project versions to match the required version.";

        // Light red tint.
        var bg = new SolidColorBrush(Colors.Red, 0.14);
        return (true, note, bg);
    }

    private static bool VersionsMatch(string left, string right)
    {
        // Prefer NuGet's version semantics.
        if (NuGetVersion.TryParse(left, out var lv) && NuGetVersion.TryParse(right, out var rv))
            return lv == rv;

        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
