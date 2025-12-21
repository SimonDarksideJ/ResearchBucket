using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonoGameHub.Core.Services;

public sealed class DotNetWorkloadState : INotifyPropertyChanged
{
    private readonly SettingsStore _settingsStore;
    private readonly DotNetWorkloadManager _workloads;

    public DotNetWorkloadState(SettingsStore settingsStore, DotNetWorkloadManager workloads)
    {
        _settingsStore = settingsStore;
        _workloads = workloads;

        _settingsStore.SettingsChanged += (_, _) => RecomputeDesiredDiff();

        // Best-effort refresh at startup.
        _ = RefreshAsync(null, CancellationToken.None);
    }

    public IReadOnlyList<string> InstalledWorkloads
    {
        get => _installedWorkloads;
        private set => SetProperty(ref _installedWorkloads, value);
    }

    public IReadOnlyList<string> AvailableWorkloads
    {
        get => _availableWorkloads;
        private set => SetProperty(ref _availableWorkloads, value);
    }

    public bool UpdatesAvailable
    {
        get => _updatesAvailable;
        private set => SetProperty(ref _updatesAvailable, value);
    }

    public IReadOnlyList<string> MissingDesiredWorkloads
    {
        get => _missingDesiredWorkloads;
        private set => SetProperty(ref _missingDesiredWorkloads, value);
    }

    public async Task RefreshAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _refreshInFlight, 1) == 1)
            return;

        try
        {
            var installed = await _workloads.ListInstalledAsync(progress, cancellationToken);
            InstalledWorkloads = installed.InstalledWorkloads;
            UpdatesAvailable = installed.UpdatesAvailable;

            // For UI selection, we deliberately surface the installed set (from `dotnet workload list`).
            // This keeps the Settings screen aligned with the user's current machine state.
            AvailableWorkloads = InstalledWorkloads;

            RecomputeDesiredDiff();
        }
        catch
        {
            InstalledWorkloads = Array.Empty<string>();
            UpdatesAvailable = false;
            AvailableWorkloads = Array.Empty<string>();
            RecomputeDesiredDiff();
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInFlight, 0);
        }
    }

    public bool IsWorkloadInstalled(string workloadId)
    {
        if (string.IsNullOrWhiteSpace(workloadId))
            return true;

        return InstalledWorkloads.Contains(workloadId, StringComparer.OrdinalIgnoreCase);
    }

    private void RecomputeDesiredDiff()
    {
        var desired = _settingsStore.Load().DesiredWorkloads ?? Array.Empty<string>();
        var missing = desired
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(w => !InstalledWorkloads.Contains(w, StringComparer.OrdinalIgnoreCase))
            .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        MissingDesiredWorkloads = missing;
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

    private IReadOnlyList<string> _installedWorkloads = Array.Empty<string>();
    private IReadOnlyList<string> _availableWorkloads = Array.Empty<string>();
    private IReadOnlyList<string> _missingDesiredWorkloads = Array.Empty<string>();
    private bool _updatesAvailable;

    private int _refreshInFlight;
}
