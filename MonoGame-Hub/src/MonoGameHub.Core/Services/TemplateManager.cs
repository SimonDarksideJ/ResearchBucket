using MonoGameHub.Core.Models;
using NuGet.Versioning;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace MonoGameHub.Core.Services;

public sealed class TemplateManager
{
    private static readonly HttpClient Http = new();

    private readonly DotNetCli _dotnet;

    public TemplateManager(DotNetCli dotnet)
    {
        _dotnet = dotnet;
    }

    public async Task<IReadOnlyList<string>> ListInstalledMonoGameTemplatesAsync(
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        var progress = new Progress<string>(line =>
        {
            lines.Add(line);
            output?.Report(line);
        });

        var env = BuildEnv(nugetPackagesFolder);

        // `dotnet new list` exists on newer SDKs.
        var exit = await _dotnet.RunAsync(new[] { "new", "list" }, null, env, progress, cancellationToken);
        if (exit != 0)
            return [];

        return lines
            .Where(l => l.Contains("MonoGame", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<TemplateInfo>> ListMonoGameTemplateOptionsAsync(
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        // NOTE: We intentionally do NOT parse `dotnet new list` for names.
        // The CLI renders a fixed-width table and truncates long names with "..." depending on console width.
        // Instead we:
        //  1) read installed template pack IDs + versions from `dotnet new uninstall`
        //  2) parse template.json from each pack to get full, un-truncated names

        var inventory = await GetInstalledTemplateInventoryAsync(nugetPackagesFolder, output, cancellationToken);
        if (inventory.PackageIdToVersion.Count == 0)
            return [];

        var results = new List<TemplateInfo>();

        foreach (var (packageId, version) in inventory.PackageIdToVersion)
        {
            // Keep this conservative: only attempt packs that look like template packs.
            // (e.g. dotnet-mgcb is a tool and can appear in the uninstall output too.)
            if (!packageId.Contains("templates", StringComparison.OrdinalIgnoreCase))
                continue;

            // We only surface MonoGame templates by default; still parse the pack and filter at the template level.
            if (!packageId.Contains("MonoGame", StringComparison.OrdinalIgnoreCase))
                continue;

            var templates = await ListTemplatesInTemplatePackAsync(
                packageId,
                version,
                nugetPackagesFolder,
                output,
                cancellationToken);

            foreach (var t in templates)
            {
                // Version shown per template is the template pack version.
                results.Add(new TemplateInfo(t.Name, t.TemplateId, version));
            }
        }

        return results
            .Where(t => t.Name.Contains("MonoGame", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(t => t.TemplateId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<string?> GetInstalledTemplatePackVersionAsync(
        string templatePackPackageId,
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        var progress = new Progress<string>(line =>
        {
            lines.Add(line);
            output?.Report(line);
        });

        var env = BuildEnv(nugetPackagesFolder);
        var exit = await _dotnet.RunAsync(new[] { "new", "uninstall" }, null, env, progress, cancellationToken);
        if (exit != 0)
            return null;

        var inTargetBlock = false;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Package id appears as a standalone line.
            if (!trimmed.Contains(':')
                && !trimmed.Equals("Currently installed items:", StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals("Details:", StringComparison.OrdinalIgnoreCase))
            {
                inTargetBlock = trimmed.Equals(templatePackPackageId, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inTargetBlock && trimmed.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring("Version:".Length).Trim();
        }

        return null;
    }

    public async Task<IReadOnlyList<TemplatePackTemplateInfo>> ListTemplatesInTemplatePackAsync(
        string templatePackPackageId,
        string templatePackVersion,
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        // Prefer local global-packages cache (covers the currently installed version and offline/local-source installs).
        var localRoot = TryGetInstalledPackageRoot(templatePackPackageId, templatePackVersion, nugetPackagesFolder);
        var useLocal = localRoot is not null && Directory.Exists(localRoot);

        // Otherwise, download + extract the template pack to a temp cache.
        var extractRoot = useLocal ? localRoot! : GetTemplatePackCacheFolder(templatePackPackageId, templatePackVersion);
        var marker = Path.Combine(extractRoot, ".extracted");

        try
        {
            if (!useLocal && !File.Exists(marker))
            {
                Directory.CreateDirectory(extractRoot);

                var nupkgPath = Path.Combine(extractRoot, "pack.nupkg");
                if (!File.Exists(nupkgPath))
                {
                    var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                    output?.Report($"Downloading {templatePackPackageId}::{templatePackVersion} ...");

                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var file = File.Create(nupkgPath);
                    await stream.CopyToAsync(file, cancellationToken);
                }

                output?.Report("Extracting template pack...");
                try
                {
                    ZipFile.ExtractToDirectory(nupkgPath, extractRoot, overwriteFiles: true);
                }
                catch (InvalidDataException)
                {
                    output?.Report("Template pack archive appears corrupt; re-downloading...");

                    TryDeleteFile(marker);
                    TryDeleteFile(nupkgPath);
                    TryDeleteDirectoryContents(extractRoot);
                    Directory.CreateDirectory(extractRoot);

                    var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var file = File.Create(nupkgPath);
                    await stream.CopyToAsync(file, cancellationToken);

                    ZipFile.ExtractToDirectory(nupkgPath, extractRoot, overwriteFiles: true);
                }
                File.WriteAllText(marker, DateTimeOffset.UtcNow.ToString("O"));
            }

            var templates = await ParseTemplatesFromFolderAsync(extractRoot, output, cancellationToken);

            if (templates.Count == 0)
                output?.Report($"No templates discovered under: {extractRoot}");

            // If we're using the temp cache and it claims to be extracted but yields 0 templates,
            // treat it as stale/corrupt and retry once.
            if (templates.Count == 0 && !useLocal && File.Exists(marker))
            {
                output?.Report("Template cache appears empty; clearing and re-extracting...");

                try
                {
                    Directory.Delete(extractRoot, recursive: true);
                }
                catch
                {
                    // Best-effort. If deletion fails, we'll fall through and return 0.
                }

                if (!Directory.Exists(extractRoot))
                    Directory.CreateDirectory(extractRoot);

                var nupkgPath = Path.Combine(extractRoot, "pack.nupkg");
                if (!File.Exists(nupkgPath))
                {
                    var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                    output?.Report($"Downloading {templatePackPackageId}::{templatePackVersion} ...");

                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var file = File.Create(nupkgPath);
                    await stream.CopyToAsync(file, cancellationToken);
                }

                output?.Report("Extracting template pack...");
                try
                {
                    ZipFile.ExtractToDirectory(nupkgPath, extractRoot, overwriteFiles: true);
                }
                catch (InvalidDataException)
                {
                    output?.Report("Template pack archive appears corrupt during re-extract; re-downloading...");

                    TryDeleteFile(marker);
                    TryDeleteFile(nupkgPath);
                    TryDeleteDirectoryContents(extractRoot);
                    Directory.CreateDirectory(extractRoot);

                    var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var file = File.Create(nupkgPath);
                    await stream.CopyToAsync(file, cancellationToken);

                    ZipFile.ExtractToDirectory(nupkgPath, extractRoot, overwriteFiles: true);
                }
                File.WriteAllText(marker, DateTimeOffset.UtcNow.ToString("O"));

                templates = await ParseTemplatesFromFolderAsync(extractRoot, output, cancellationToken);
                if (templates.Count == 0)
                    output?.Report("Re-extract completed but still found 0 templates.");
            }

            // Some environments may have the package folder present but not extracted template files.
            // In that case, fall back to extracting the local nupkg (if present) or downloading to our temp cache and parse there.
            if (templates.Count == 0 && useLocal)
            {
                var nupkgCandidate = Directory
                    .EnumerateFiles(extractRoot, "*.nupkg", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                var fallbackRoot = GetTemplatePackCacheFolder(templatePackPackageId, templatePackVersion);
                var fallbackMarker = Path.Combine(fallbackRoot, ".extracted");

                if (!File.Exists(fallbackMarker))
                {
                    Directory.CreateDirectory(fallbackRoot);

                    if (!string.IsNullOrWhiteSpace(nupkgCandidate))
                    {
                        output?.Report("Extracting template pack from local .nupkg...");
                        try
                        {
                            ZipFile.ExtractToDirectory(nupkgCandidate, fallbackRoot, overwriteFiles: true);
                            File.WriteAllText(fallbackMarker, DateTimeOffset.UtcNow.ToString("O"));
                        }
                        catch (InvalidDataException)
                        {
                            output?.Report("Local .nupkg appears corrupt; will download instead.");
                            TryDeleteFile(fallbackMarker);
                            TryDeleteDirectoryContents(fallbackRoot);
                        }
                    }
                    else
                    {
                        var nupkgPath = Path.Combine(fallbackRoot, "pack.nupkg");
                        if (!File.Exists(nupkgPath))
                        {
                            var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                            output?.Report($"Downloading {templatePackPackageId}::{templatePackVersion} for template parsing...");

                            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                            response.EnsureSuccessStatusCode();

                            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                            await using var file = File.Create(nupkgPath);
                            await stream.CopyToAsync(file, cancellationToken);
                        }

                        output?.Report("Extracting downloaded template pack...");
                        try
                        {
                            ZipFile.ExtractToDirectory(nupkgPath, fallbackRoot, overwriteFiles: true);
                            File.WriteAllText(fallbackMarker, DateTimeOffset.UtcNow.ToString("O"));
                        }
                        catch (InvalidDataException)
                        {
                            output?.Report("Downloaded template pack appears corrupt; retrying download...");
                            TryDeleteFile(fallbackMarker);
                            TryDeleteFile(nupkgPath);
                            TryDeleteDirectoryContents(fallbackRoot);
                            Directory.CreateDirectory(fallbackRoot);

                            var url = GetFlatContainerNupkgUrl(templatePackPackageId, templatePackVersion);
                            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                            response.EnsureSuccessStatusCode();

                            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                            await using var file = File.Create(nupkgPath);
                            await stream.CopyToAsync(file, cancellationToken);

                            ZipFile.ExtractToDirectory(nupkgPath, fallbackRoot, overwriteFiles: true);
                            File.WriteAllText(fallbackMarker, DateTimeOffset.UtcNow.ToString("O"));
                        }
                    }
                }

                templates = await ParseTemplatesFromFolderAsync(fallbackRoot, output, cancellationToken);

                if (templates.Count == 0)
                    output?.Report($"No templates discovered under fallback root: {fallbackRoot}");
            }

            return templates
                .DistinctBy(t => t.TemplateId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            // Selection changes cancel in-flight parsing; treat as normal.
            throw;
        }
        catch (Exception ex)
        {
            output?.Report($"Template pack parse failed: {ex.Message}");
            return [];
        }
    }

    private static async Task<List<TemplatePackTemplateInfo>> ParseTemplatesFromFolderAsync(
        string extractRoot,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var templates = new List<TemplatePackTemplateInfo>();

        if (!Directory.Exists(extractRoot))
            return templates;

        foreach (var file in Directory.EnumerateFiles(extractRoot, "template.json", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                return templates;

            // We only care about dotnet templates.
            if (!file.Contains(".template.config", StringComparison.OrdinalIgnoreCase))
                continue;

            string json;
            try
            {
                json = await File.ReadAllTextAsync(file, cancellationToken);
            }
            catch (Exception ex)
            {
                // Cancellation is expected when users change selection quickly.
                if (ex is OperationCanceledException || ex is TaskCanceledException || cancellationToken.IsCancellationRequested)
                    return templates;

                output?.Report($"Failed to read template.json: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(json))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                var shortName = TryGetShortName(root);

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
                    continue;

                templates.Add(new TemplatePackTemplateInfo(name.Trim(), shortName.Trim()));
            }
            catch
            {
                // Ignore malformed templates.
            }
        }

        return templates;
    }

    public Task<IReadOnlyList<TemplatePackTemplateInfo>> ListTemplatesInTemplatePackAsync(
        string templatePackPackageId,
        string templatePackVersion,
        IProgress<string>? output,
        CancellationToken cancellationToken)
        => ListTemplatesInTemplatePackAsync(templatePackPackageId, templatePackVersion, nugetPackagesFolder: null, output, cancellationToken);

    public Task<int> UninstallAsync(
        string packageId,
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
        => _dotnet.RunAsync(new[] { "new", "uninstall", packageId }, null, BuildEnv(nugetPackagesFolder), output, cancellationToken);

    public Task<int> InstallAsync(
        string packageId,
        string version,
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
        => _dotnet.RunAsync(new[] { "new", "install", $"{packageId}::{version}" }, null, BuildEnv(nugetPackagesFolder), output, cancellationToken);

    public async Task<IReadOnlyList<TemplateVersionInfo>> GetAvailableVersionsAsync(
        string packageId,
        NuGetVersionResolver resolver,
        CancellationToken cancellationToken)
    {
        var versions = await resolver.GetAllVersionsAsync(packageId, cancellationToken);

        // Sort by NuGet version semantics first (major/minor/patch/build), which also correctly places
        // stable releases ahead of prereleases for the same base version, and compares numeric prerelease
        // segments numerically (develop.13 > develop.8).
        static int CompareNuGetVersionsNullable(NuGetVersion? a, NuGetVersion? b)
        {
            if (a is null && b is null) return 0;
            if (a is null) return -1;
            if (b is null) return 1;
            return a.CompareTo(b);
        }

        var parsed = versions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(v => (Original: v, Parsed: NuGetVersion.TryParse(v, out var nv) ? nv : null))
            .ToList();

        return parsed
            .OrderByDescending(v => v.Parsed, Comparer<NuGetVersion?>.Create(CompareNuGetVersionsNullable))
            .ThenByDescending(v => v.Original, StringComparer.OrdinalIgnoreCase)
            .Select(v => new TemplateVersionInfo(v.Original, IsPrerelease(v.Original)))
            .ToList();
    }

    private static bool IsPrerelease(string version)
        => version.Contains('-', StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string?> BuildEnv(string? nugetPackagesFolder)
        => string.IsNullOrWhiteSpace(nugetPackagesFolder)
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?> { ["NUGET_PACKAGES"] = nugetPackagesFolder };

    private static string GetTemplatePackCacheFolder(string packageId, string version)
    {
        // Cache extracted packs under temp (fast + safe; no installs required).
        var safeId = packageId.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        var safeVersion = version.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        return Path.Combine(Path.GetTempPath(), "MonoGameHub", "templatepacks", safeId, safeVersion);
    }

    private static string? TryGetInstalledPackageRoot(string packageId, string version, string? nugetPackagesFolder)
    {
        // Global packages folder layout:
        //   <root>/<packageId lower>/<version lower>/
        var root = string.IsNullOrWhiteSpace(nugetPackagesFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            : nugetPackagesFolder;

        var id = packageId.ToLowerInvariant();
        var v = version.ToLowerInvariant();
        var candidate = Path.Combine(root, id, v);
        return Directory.Exists(candidate) ? candidate : null;
    }

    private static string GetFlatContainerNupkgUrl(string packageId, string version)
    {
        // NuGet flat container is lower-case.
        var id = packageId.ToLowerInvariant();
        var v = version.ToLowerInvariant();
        return $"https://api.nuget.org/v3-flatcontainer/{id}/{v}/{id}.{v}.nupkg";
    }

    private static string? TryGetShortName(JsonElement root)
    {
        if (!root.TryGetProperty("shortName", out var sn))
            return null;

        if (sn.ValueKind == JsonValueKind.String)
            return sn.GetString();

        if (sn.ValueKind == JsonValueKind.Array)
        {
            var names = sn
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return names.Length == 0 ? null : string.Join(", ", names!);
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort.
        }
    }

    private static void TryDeleteDirectoryContents(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
                return;

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
            }

            foreach (var child in Directory.EnumerateFileSystemEntries(directory))
            {
                try
                {
                    if (Directory.Exists(child))
                        Directory.Delete(child, recursive: true);
                    else
                        File.Delete(child);
                }
                catch
                {
                    // Keep going.
                }
            }
        }
        catch
        {
            // Best-effort.
        }
    }

    private static IReadOnlyList<TemplateInfo> ParseDotNetNewList(
        IReadOnlyList<string> lines,
        IReadOnlyDictionary<string, string> templateIdToVersion)
    {
        // Typical output:
        // Template Name                                 Short Name      Language    Tags
        // --------------------------------------------  --------------  ----------  ----------------------
        // MonoGame DesktopGL Application                mgdesktopgl     [C#]        MonoGame/Game/Desktop
        // ...

        var result = new List<TemplateInfo>();

        // Find header line (contains "Template Name" and "Short Name").
        var headerIndex = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("Template Name", StringComparison.OrdinalIgnoreCase)
                && line.Contains("Short Name", StringComparison.OrdinalIgnoreCase))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0 || headerIndex + 2 >= lines.Count)
            return result;

        // Data starts after separator line.
        for (var i = headerIndex + 2; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Split by 2+ spaces to separate columns.
            var parts = line
                .Split("  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length < 2)
                continue;

            var name = parts[0];
            var templateId = parts[1];

            if (templateId.Equals("Short", StringComparison.OrdinalIgnoreCase))
                continue;

            templateIdToVersion.TryGetValue(templateId, out var version);
            result.Add(new TemplateInfo(name, templateId, version ?? string.Empty));
        }

        return result;
    }

    private sealed record InstalledTemplateInventory(
        IReadOnlyDictionary<string, string> TemplateIdToVersion,
        IReadOnlyDictionary<string, string> PackageIdToVersion);

    private async Task<InstalledTemplateInventory> GetInstalledTemplateInventoryAsync(
        string? nugetPackagesFolder,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        // `dotnet new uninstall` prints installed *template packages* and the templates they include.
        // We parse that output to:
        //   - map template short name -> package version
        //   - map package id -> package version
        var lines = new List<string>();
        var progress = new Progress<string>(line =>
        {
            lines.Add(line);
            output?.Report(line);
        });

        var env = BuildEnv(nugetPackagesFolder);
        var exit = await _dotnet.RunAsync(new[] { "new", "uninstall" }, null, env, progress, cancellationToken);
        if (exit != 0)
            return new InstalledTemplateInventory(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        return ParseDotNetNewUninstall(lines);
    }

    private static InstalledTemplateInventory ParseDotNetNewUninstall(IReadOnlyList<string> lines)
    {
        // Example:
        // Currently installed items:
        //    MonoGame.Templates.CSharp
        //       Version: 3.8.5-develop.13
        //       ...
        //       Templates:
        //          MonoGame Cross-Platform Desktop Application (mgdesktopgl) C#
        //          ...

        var templateIdToVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var packageIdToVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? currentPackageId = null;
        string? currentVersion = null;
        var inTemplates = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Equals("Templates:", StringComparison.OrdinalIgnoreCase))
            {
                inTemplates = true;
                continue;
            }

            if (inTemplates)
            {
                if (trimmed.StartsWith("Uninstall Command:", StringComparison.OrdinalIgnoreCase))
                {
                    inTemplates = false;
                    continue;
                }

                // Template lines typically include a short name in parentheses.
                var openParen = trimmed.LastIndexOf('(');
                var closeParen = trimmed.LastIndexOf(')');
                if (openParen >= 0 && closeParen > openParen)
                {
                    var templateId = trimmed.Substring(openParen + 1, closeParen - openParen - 1).Trim();
                    if (!string.IsNullOrWhiteSpace(templateId) && !string.IsNullOrWhiteSpace(currentVersion))
                        templateIdToVersion[templateId] = currentVersion;
                }

                continue;
            }

            if (trimmed.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
            {
                currentVersion = trimmed.Substring("Version:".Length).Trim();

                if (!string.IsNullOrWhiteSpace(currentPackageId) && !string.IsNullOrWhiteSpace(currentVersion))
                    packageIdToVersion[currentPackageId] = currentVersion;
                continue;
            }

            // New package block lines are usually indented but are not key/value lines.
            if (!trimmed.Contains(':')
                && !trimmed.Equals("Currently installed items:", StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals("Details:", StringComparison.OrdinalIgnoreCase))
            {
                currentPackageId = trimmed;
                currentVersion = null;
                inTemplates = false;
                continue;
            }

            _ = currentPackageId;
        }

        return new InstalledTemplateInventory(templateIdToVersion, packageIdToVersion);
    }
}
