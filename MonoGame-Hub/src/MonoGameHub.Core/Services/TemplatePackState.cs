using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonoGameHub.Core.Services;

public sealed class TemplatePackState : INotifyPropertyChanged
{
    private readonly SettingsStore _settingsStore;
    private readonly TemplateManager _templates;

    public TemplatePackState(SettingsStore settingsStore, TemplateManager templates)
    {
        _settingsStore = settingsStore;
        _templates = templates;

        ReloadFromSettings();
    }

    public string TemplatePackageId
    {
        get => _templatePackageId;
        private set => SetProperty(ref _templatePackageId, value);
    }

    public string InstalledTemplatePackVersion
    {
        get => _installedTemplatePackVersion;
        private set => SetProperty(ref _installedTemplatePackVersion, value);
    }

    public void ReloadFromSettings()
    {
        var settings = _settingsStore.Load();
        TemplatePackageId = string.IsNullOrWhiteSpace(settings.MonoGameTemplatePackageId)
            ? DefaultTemplatePackageId
            : settings.MonoGameTemplatePackageId;
    }

    public void SetTemplatePackageId(string? packageId)
    {
        var normalized = string.IsNullOrWhiteSpace(packageId) ? DefaultTemplatePackageId : packageId;

        if (TemplatePackageId.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            return;

        TemplatePackageId = normalized;

        var settings = _settingsStore.Load();
        settings = settings with { MonoGameTemplatePackageId = TemplatePackageId };
        _settingsStore.Save(settings);
    }

    public async Task RefreshInstalledVersionAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        // Prevent multiple callers from spamming this expensive call.
        if (Interlocked.Exchange(ref _refreshInstalledInFlight, 1) == 1)
            return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastInstalledRefreshUtc is not null
                && (now - _lastInstalledRefreshUtc.Value) < TimeSpan.FromSeconds(1))
            {
                return;
            }

            _lastInstalledRefreshUtc = now;

            var settings = _settingsStore.Load();
            progress ??= new Progress<string>(_ => { });

            var v = await _templates.GetInstalledTemplatePackVersionAsync(
                TemplatePackageId,
                settings.NuGetPackagesFolder,
                progress,
                cancellationToken);

            InstalledTemplatePackVersion = v ?? string.Empty;
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInstalledInFlight, 0);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private const string DefaultTemplatePackageId = "MonoGame.Templates.CSharp";
    private string _templatePackageId = DefaultTemplatePackageId;
    private string _installedTemplatePackVersion = string.Empty;

    private int _refreshInstalledInFlight;
    private DateTimeOffset? _lastInstalledRefreshUtc;
}
