using System.Xml.Linq;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

public sealed class ProjectScanner
{
    public async Task<IReadOnlyList<ProjectInfo>> ScanAsync(
        string rootFolder,
        Func<ProjectInfo, Task<string?>>? resolveVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            return [];

        rootFolder = Path.GetFullPath(rootFolder);
        var normalizedRootPrefix = NormalizeDirectoryPrefix(rootFolder);

        var csprojPaths = Directory.EnumerateFiles(rootFolder, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Deduplicate and collapse projects under a solution into a single entry.
        var standalone = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        var solutions = new Dictionary<string, SolutionAggregate>(StringComparer.OrdinalIgnoreCase);

        var solutionReferenceCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var csprojPath in csprojPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProjectInfo? info;

            try
            {
                info = InspectProject(csprojPath);
            }
            catch
            {
                continue;
            }

            if (info is null)
                continue;

            var enclosingSolution = FindEnclosingSolutionForProject(
                csprojPath,
                normalizedRootPrefix,
                solutionReferenceCache);

            if (!string.IsNullOrWhiteSpace(enclosingSolution))
            {
                if (!solutions.TryGetValue(enclosingSolution, out var agg))
                    agg = new SolutionAggregate(enclosingSolution);

                agg.AddProject(info);
                solutions[enclosingSolution] = agg;
                continue;
            }

            // No solution found: keep project itself.
            standalone[info.ProjectPath] = info;
        }

        var results = new List<ProjectInfo>(standalone.Count + solutions.Count);
        results.AddRange(standalone.Values);
        results.AddRange(solutions.Values.Select(a => a.ToProjectInfo()));

        // Final safety net: ensure unique paths in returned list.
        results = results
            .GroupBy(r => Path.GetFullPath(r.ProjectPath), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (resolveVersion is not null)
        {
            for (var i = 0; i < results.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = results[i];
                var resolved = await resolveVersion(info);
                results[i] = info with { ResolvedMonoGameVersion = resolved };
            }
        }

        return results;
    }

    private static string NormalizeDirectoryPrefix(string folder)
    {
        var full = Path.GetFullPath(folder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full + Path.DirectorySeparatorChar;
    }

    private static bool IsSameOrUnderRoot(string folder, string normalizedRootPrefix)
    {
        var full = NormalizeDirectoryPrefix(folder);
        return full.StartsWith(normalizedRootPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindEnclosingSolutionForProject(
        string csprojPath,
        string normalizedRootPrefix,
        Dictionary<string, string?> solutionReferenceCache)
    {
        var csprojFull = Path.GetFullPath(csprojPath);
        var csprojDir = Path.GetDirectoryName(csprojFull);
        if (string.IsNullOrWhiteSpace(csprojDir))
            return null;

        var current = csprojDir;
        while (!string.IsNullOrWhiteSpace(current) && IsSameOrUnderRoot(current, normalizedRootPrefix))
        {
            List<string> candidates;
            try
            {
                candidates = Directory
                    .EnumerateFiles(current, "*.sln", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                candidates = new List<string>();
            }

            if (candidates.Count > 0)
            {
                // Prefer a solution that actually references this project.
                foreach (var slnPath in candidates)
                {
                    if (SolutionReferencesProject(slnPath, csprojFull, solutionReferenceCache))
                        return slnPath;
                }

                // Otherwise fall back to the only solution in the folder (if unambiguous).
                if (candidates.Count == 1)
                    return candidates[0];
            }

            var parent = Directory.GetParent(current)?.FullName;
            if (string.IsNullOrWhiteSpace(parent) || parent.Equals(current, StringComparison.OrdinalIgnoreCase))
                break;

            current = parent;
        }

        return null;
    }

    private static bool SolutionReferencesProject(
        string slnPath,
        string csprojFullPath,
        Dictionary<string, string?> solutionReferenceCache)
    {
        // Cache solution text to avoid rereading for every project.
        if (!solutionReferenceCache.TryGetValue(slnPath, out var slnText))
        {
            try
            {
                slnText = File.ReadAllText(slnPath);
            }
            catch
            {
                slnText = null;
            }

            solutionReferenceCache[slnPath] = slnText;
        }

        if (string.IsNullOrWhiteSpace(slnText))
            return false;

        var slnDir = Path.GetDirectoryName(Path.GetFullPath(slnPath));
        if (string.IsNullOrWhiteSpace(slnDir))
            return false;

        string rel;
        try
        {
            rel = Path.GetRelativePath(slnDir, csprojFullPath);
        }
        catch
        {
            return false;
        }

        var relBackslash = rel.Replace('/', '\\');

        // .sln uses quoted relative paths; keep this check cheap and case-insensitive.
        return slnText.Contains(relBackslash, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SolutionAggregate
    {
        private readonly string _solutionPath;
        private readonly HashSet<string> _platforms = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _versionSpecs = new(StringComparer.OrdinalIgnoreCase);
        private bool _usesLegacyMgcb;
        private bool _usesNewPipeline;
        private DateTimeOffset _lastWriteUtc;

        public SolutionAggregate(string solutionPath)
        {
            _solutionPath = Path.GetFullPath(solutionPath);
            _lastWriteUtc = TryGetLastWriteUtc(_solutionPath);
        }

        public void AddProject(ProjectInfo project)
        {
            foreach (var p in project.Platforms)
                _platforms.Add(p);

            if (!string.IsNullOrWhiteSpace(project.MonoGameVersionSpec))
                _versionSpecs.Add(project.MonoGameVersionSpec);

            _usesLegacyMgcb |= project.UsesLegacyMgcb;
            _usesNewPipeline |= project.UsesNewPipeline;

            if (project.ProjectFileLastWriteTimeUtc > _lastWriteUtc)
                _lastWriteUtc = project.ProjectFileLastWriteTimeUtc;
        }

        public ProjectInfo ToProjectInfo()
        {
            var name = Path.GetFileNameWithoutExtension(_solutionPath);
            var versionSpec = _versionSpecs.Count == 1 ? _versionSpecs.First() : null;

            return new ProjectInfo(
                Name: name,
                ProjectPath: _solutionPath,
                ProjectFileLastWriteTimeUtc: _lastWriteUtc,
                LastOpenedUtc: null,
                Platforms: _platforms.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList(),
                UsesLegacyMgcb: _usesLegacyMgcb,
                UsesNewPipeline: _usesNewPipeline,
                MonoGameVersionSpec: versionSpec,
                ResolvedMonoGameVersion: null);
        }
    }

    private static DateTimeOffset TryGetLastWriteUtc(string filePath)
    {
        try
        {
            return new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);
        }
        catch
        {
            return DateTimeOffset.UtcNow;
        }
    }

    private static ProjectInfo? InspectProject(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);

        var packageRefs = doc
            .Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => new
            {
                Include = e.Attribute("Include")?.Value,
                Version = e.Attribute("Version")?.Value ?? e.Element(e.Name.Namespace + "Version")?.Value
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.Include))
            .ToList();

        var includes = packageRefs
            .Select(p => p.Include!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isMonoGame = includes.Any(i => i.StartsWith("MonoGame.Framework", StringComparison.OrdinalIgnoreCase))
            || includes.Contains("MonoGame.Framework", StringComparer.OrdinalIgnoreCase);

        // Also treat pipeline/tooling-only projects as relevant.
        var hasPipeline = includes.Any(i => i.Contains("MonoGame.Pipeline", StringComparison.OrdinalIgnoreCase));
        var hasLegacyBuilderTaskPackage = includes.Any(i => i.Contains("MonoGame.Content.Builder.Task", StringComparison.OrdinalIgnoreCase));
        var hasContentReference = includes.Any(i => i.Contains("MonoGame.Content.Reference", StringComparison.OrdinalIgnoreCase));

        // Detect actual MGCB usage via MSBuild items (preferred over checking for .mgcb files on disk).
        var hasMonoGameContentReferenceItem = doc
            .Descendants()
            .Any(e => e.Name.LocalName.Equals("MonoGameContentReference", StringComparison.OrdinalIgnoreCase));

        // Some projects reference the builder task via explicit MSBuild imports/UsingTask entries
        // (and may inject MonoGameContentReference from imported .targets instead of the csproj).
        var hasLegacyBuilderTaskMsbuild = doc
            .Descendants()
            .Any(e =>
            {
                if (e.Name.LocalName.Equals("Import", StringComparison.OrdinalIgnoreCase))
                {
                    var projectAttr = e.Attribute("Project")?.Value;
                    return !string.IsNullOrWhiteSpace(projectAttr)
                        && projectAttr.Contains("MonoGame.Content.Builder.Task", StringComparison.OrdinalIgnoreCase);
                }

                if (e.Name.LocalName.Equals("UsingTask", StringComparison.OrdinalIgnoreCase))
                {
                    var taskNameAttr = e.Attribute("TaskName")?.Value;
                    var assemblyFileAttr = e.Attribute("AssemblyFile")?.Value;
                    return (!string.IsNullOrWhiteSpace(taskNameAttr)
                            && taskNameAttr.Contains("MonoGame.Content.Builder.Task", StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(assemblyFileAttr)
                            && assemblyFileAttr.Contains("MonoGame.Content.Builder.Task", StringComparison.OrdinalIgnoreCase));
                }

                return false;
            });

        if (!isMonoGame && !hasPipeline && !hasLegacyBuilderTaskPackage && !hasContentReference)
            return null;

        var platforms = new List<string>();

        if (includes.Contains("MonoGame.Framework.DesktopGL", StringComparer.OrdinalIgnoreCase))
            platforms.Add("DesktopGL");

        if (includes.Contains("MonoGame.Framework.WindowsDX", StringComparer.OrdinalIgnoreCase))
            platforms.Add("WindowsDX");

        if (hasPipeline)
            platforms.Add("Pipeline");

        var versionSpec = packageRefs
            .Where(p => p.Include is not null && p.Include.StartsWith("MonoGame.Framework", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Version)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        // Legacy MGCB indicators: MSBuild content reference, builder task (package or explicit MSBuild reference),
        // or the content reference package.
        var usesLegacyMgcb = hasMonoGameContentReferenceItem || hasLegacyBuilderTaskPackage || hasLegacyBuilderTaskMsbuild || hasContentReference;
        var usesNewPipeline = hasPipeline;

        var name = Path.GetFileNameWithoutExtension(csprojPath);

        var lastWriteUtc = TryGetLastWriteUtc(csprojPath);

        return new ProjectInfo(
            Name: name,
            ProjectPath: csprojPath,
            ProjectFileLastWriteTimeUtc: lastWriteUtc,
            LastOpenedUtc: null,
            Platforms: platforms,
            UsesLegacyMgcb: usesLegacyMgcb,
            UsesNewPipeline: usesNewPipeline,
            MonoGameVersionSpec: versionSpec,
            ResolvedMonoGameVersion: null);
    }
}
