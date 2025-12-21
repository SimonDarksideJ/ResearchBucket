using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonoGameHub.Core.Services;

public sealed class ToolingSetupState : INotifyPropertyChanged
{
    private readonly TemplatePackState _templatePack;
    private readonly DotNetWorkloadState _workloads;

    public ToolingSetupState(TemplatePackState templatePack, DotNetWorkloadState workloads)
    {
        _templatePack = templatePack;
        _workloads = workloads;

        _templatePack.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TemplatePackState.InstalledTemplatePackVersion))
            {
                OnPropertyChanged(nameof(InstalledLine));
            }
        };

        _workloads.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(DotNetWorkloadState.InstalledWorkloads)
                or nameof(DotNetWorkloadState.UpdatesAvailable)
                or nameof(DotNetWorkloadState.MissingDesiredWorkloads))
            {
                OnPropertyChanged(nameof(WorkloadsLine));
                OnPropertyChanged(nameof(IsWorkloadUpdateRecommended));
            }
        };
    }

    public string InstalledLine
    {
        get
        {
            var v = string.IsNullOrWhiteSpace(_templatePack.InstalledTemplatePackVersion)
                ? "(unknown)"
                : _templatePack.InstalledTemplatePackVersion;

            return $"Installed: {v}";
        }
    }

    public string WorkloadsLine
    {
        get
        {
            var installed = _workloads.InstalledWorkloads;
            var suffix = IsWorkloadUpdateRecommended ? " *Update Available" : string.Empty;

            if (installed.Count == 0)
                return $"Workloads: (none){suffix}";

            // Two-per-line formatting for readability.
            // Example:
            //  Workloads: android, ios
            //             maccatalyst, maui-windows
            var pairs = installed
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToArray();

            var lines = new List<string>();
            for (var i = 0; i < pairs.Length; i += 2)
            {
                var chunk = (i + 1 < pairs.Length)
                    ? $"{pairs[i]}, {pairs[i + 1]}"
                    : pairs[i];
                lines.Add(chunk);
            }

            var formatted = lines.Count == 1
                ? lines[0]
                : string.Join("\n          ", lines);

            return $"Workloads: {formatted}{suffix}";
        }
    }

    public bool IsWorkloadUpdateRecommended
        => _workloads.UpdatesAvailable || _workloads.MissingDesiredWorkloads.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
