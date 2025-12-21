using System.Text;

namespace MonoGameHub.Core.Services;

public sealed class DotNetWorkloadManager
{
    private readonly DotNetCli _dotnet;

    public DotNetWorkloadManager(DotNetCli dotnet)
    {
        _dotnet = dotnet;
    }

    public sealed record InstalledWorkloadsResult(
        IReadOnlyList<string> InstalledWorkloads,
        bool UpdatesAvailable);

    public async Task<InstalledWorkloadsResult> ListInstalledAsync(
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        var progress = new Progress<string>(line =>
        {
            lines.Add(line);
            output?.Report(line);
        });

        var exit = await _dotnet.RunAsync(new[] { "workload", "list" }, null, null, progress, cancellationToken);
        if (exit != 0)
            return new InstalledWorkloadsResult(Array.Empty<string>(), UpdatesAvailable: false);

        var updatesAvailable = lines.Any(l =>
            l.Contains("updates are available", StringComparison.OrdinalIgnoreCase)
            || (l.Contains("update", StringComparison.OrdinalIgnoreCase) && l.Contains("available", StringComparison.OrdinalIgnoreCase)));

        var installed = ParseFirstColumnTable(
            lines,
            headerMustContain: new[] { "workload", "id" },
            stopIfContainsAny: new[] { "run", "dotnet workload", "learn more" });

        installed = installed
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new InstalledWorkloadsResult(installed, updatesAvailable);
    }

    public async Task<IReadOnlyList<string>> SearchAvailableAsync(
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        // `dotnet workload search` lists available workloads (not just installed).
        // Not all SDKs support it; treat failures as "unknown".

        var lines = new List<string>();
        var progress = new Progress<string>(line =>
        {
            lines.Add(line);
            output?.Report(line);
        });

        var exit = await _dotnet.RunAsync(new[] { "workload", "search" }, null, null, progress, cancellationToken);
        if (exit != 0)
            return Array.Empty<string>();

        var results = ParseFirstColumnTable(
            lines,
            headerMustContain: new[] { "workload" },
            stopIfContainsAny: new[] { "run", "dotnet workload" });

        return results
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task<int> InstallAsync(
        string workloadId,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workloadId))
            throw new ArgumentException("Workload id is required", nameof(workloadId));

        return _dotnet.RunAsync(new[] { "workload", "install", workloadId }, null, null, output, cancellationToken);
    }

    public Task<int> UpdateAsync(IProgress<string>? output, CancellationToken cancellationToken)
        => _dotnet.RunAsync(new[] { "workload", "update" }, null, null, output, cancellationToken);

    private static IReadOnlyList<string> ParseFirstColumnTable(
        IReadOnlyList<string> lines,
        IReadOnlyList<string> headerMustContain,
        IReadOnlyList<string> stopIfContainsAny)
    {
        var foundHeader = false;
        var results = new List<string>();

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!foundHeader)
            {
                var normalized = line.ToLowerInvariant();
                if (headerMustContain.All(h => normalized.Contains(h, StringComparison.OrdinalIgnoreCase)))
                {
                    foundHeader = true;
                }

                continue;
            }

            var lower = line.ToLowerInvariant();
            if (stopIfContainsAny.Any(s => lower.Contains(s, StringComparison.OrdinalIgnoreCase)))
                break;

            // Skip separator lines like: "-----" or "----  ----".
            if (line.All(c => c == '-' || char.IsWhiteSpace(c)))
                continue;

            // Take first token as workload id.
            var first = ReadFirstToken(line);
            if (!string.IsNullOrWhiteSpace(first))
                results.Add(first);
        }

        return results;
    }

    private static string ReadFirstToken(string line)
    {
        var sb = new StringBuilder();
        foreach (var c in line)
        {
            if (char.IsWhiteSpace(c))
                break;

            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}
