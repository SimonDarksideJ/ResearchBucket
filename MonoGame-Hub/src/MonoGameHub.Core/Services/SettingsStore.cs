using System.Text.Json;

namespace MonoGameHub.Core.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    private HubSettings _cached = new();
    private bool _hasCache;

    public event EventHandler<HubSettings>? SettingsChanged;

    public bool SettingsFileExists => File.Exists(_settingsPath);

    public string SettingsFilePath => _settingsPath;

    public SettingsStore()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonoGameHub");

        Directory.CreateDirectory(baseDir);
        _settingsPath = Path.Combine(baseDir, "settings.json");
    }

    public HubSettings Load()
    {
        if (_hasCache)
            return _cached;

        if (!File.Exists(_settingsPath))
        {
            _cached = new HubSettings() with { RecentProjects = Array.Empty<RecentProjectEntry>() };
            _hasCache = true;
            return _cached;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            _cached = JsonSerializer.Deserialize<HubSettings>(json, JsonOptions) ?? new HubSettings();
            if (_cached.RecentProjects is null)
                _cached = _cached with { RecentProjects = Array.Empty<RecentProjectEntry>() };
            _hasCache = true;
            return _cached;
        }
        catch
        {
            _cached = new HubSettings() with { RecentProjects = Array.Empty<RecentProjectEntry>() };
            _hasCache = true;
            return _cached;
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_settingsPath))
                File.Delete(_settingsPath);
        }
        catch
        {
            // Best-effort.
        }

        _cached = new HubSettings() with { RecentProjects = Array.Empty<RecentProjectEntry>() };
        _hasCache = true;
        SettingsChanged?.Invoke(this, _cached);
    }

    public void Save(HubSettings settings)
    {
        if (_hasCache && Equals(_cached, settings))
            return;

        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
        }
        catch
        {
            // Best-effort.
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);

        _cached = settings;
        _hasCache = true;
        SettingsChanged?.Invoke(this, settings);
    }
}

public sealed record HubSettings(
    string? ProjectsRoot = null,
    string? NuGetPackagesFolder = null,
    string PreferredIde = "VSCode",
    string MonoGameTemplatePackageId = "MonoGame.Templates.CSharp",
    string ProjectsOrderingMode = "ProjectUpdated",
    bool SkipSplashScreen = false,
    int LastSeenBlogPostCount = 0,
    IReadOnlyList<RecentProjectEntry>? RecentProjects = null);

public sealed record RecentProjectEntry(
    string ProjectPath,
    DateTimeOffset LastOpenedUtc);
