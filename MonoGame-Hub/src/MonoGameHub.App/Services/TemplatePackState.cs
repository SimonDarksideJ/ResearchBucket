using CommunityToolkit.Mvvm.ComponentModel;
using MonoGameHub.Core.Services;
using System.Threading;

namespace MonoGameHub.App.Services;

public sealed partial class TemplatePackState : ObservableObject
{
    private readonly SettingsStore _settingsStore;
    private readonly TemplateManager _templates;

    public TemplatePackState(SettingsStore settingsStore, TemplateManager templates)
    {
        _settingsStore = settingsStore;
        _templates = templates;

        ReloadFromSettings();
    }

    [ObservableProperty]
    private string _templatePackageId = "MonoGame.Templates.CSharp";

    [ObservableProperty]
    private string _installedTemplatePackVersion = string.Empty;

    public void ReloadFromSettings()
    {
        var settings = _settingsStore.Load();
        TemplatePackageId = string.IsNullOrWhiteSpace(settings.MonoGameTemplatePackageId)
            ? "MonoGame.Templates.CSharp"
            : settings.MonoGameTemplatePackageId;
    }

    public void SetTemplatePackageId(string? packageId)
    {
        var normalized = string.IsNullOrWhiteSpace(packageId) ? "MonoGame.Templates.CSharp" : packageId;

        if (TemplatePackageId.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            return;

        TemplatePackageId = normalized;

        var settings = _settingsStore.Load();
        settings = settings with { MonoGameTemplatePackageId = TemplatePackageId };
        _settingsStore.Save(settings);
    }

    public async Task RefreshInstalledVersionAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        // Prevent multiple viewmodels/tabs from spamming this expensive call.
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

    private int _refreshInstalledInFlight;
    private DateTimeOffset? _lastInstalledRefreshUtc;
}
