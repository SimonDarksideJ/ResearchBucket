using System.Diagnostics;
using System.Xml.Linq;
using MonoGameHub.Core.Interfaces;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

/// <summary>
/// Service for managing MonoGame projects
/// </summary>
public class ProjectService : IProjectService
{
    private readonly INuGetService _nugetService;
    private readonly List<MonoGameProject> _projects = new();

    public ProjectService(INuGetService nugetService)
    {
        _nugetService = nugetService;
    }

    public async Task<List<MonoGameProject>> ScanFolderAsync(string path)
    {
        var projects = new List<MonoGameProject>();

        if (!Directory.Exists(path))
            return projects;

        // Find all .csproj files
        var csprojFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);

        foreach (var csprojFile in csprojFiles)
        {
            var project = await AnalyzeProjectAsync(csprojFile);
            if (project != null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }

    public async Task<MonoGameProject?> AnalyzeProjectAsync(string projectPath)
    {
        if (!File.Exists(projectPath))
            return null;

        try
        {
            var doc = await XDocument.LoadAsync(File.OpenRead(projectPath), LoadOptions.None, CancellationToken.None);
            
            // Check for MonoGame references
            var packageReferences = doc.Descendants("PackageReference")
                .Where(p => p.Attribute("Include")?.Value.Contains("MonoGame") == true)
                .ToList();

            if (!packageReferences.Any())
                return null; // Not a MonoGame project

            // Extract project information
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var projectDir = Path.GetDirectoryName(projectPath) ?? "";

            // Detect MonoGame version
            var monoGamePackage = packageReferences
                .FirstOrDefault(p => p.Attribute("Include")?.Value == "MonoGame.Framework.DesktopGL" ||
                                    p.Attribute("Include")?.Value == "MonoGame.Framework.WindowsDX");
            
            var version = monoGamePackage?.Attribute("Version")?.Value ?? "Unknown";

            // Detect project type (Legacy MGCB vs Modern Pipeline)
            var hasBuilderTask = packageReferences.Any(p => p.Attribute("Include")?.Value == "MonoGame.Content.Builder.Task");
            var hasMgcbFile = Directory.GetFiles(projectDir, "*.mgcb", SearchOption.AllDirectories).Any();
            var projectType = (hasBuilderTask || hasMgcbFile) ? ProjectType.LegacyMGCB : ProjectType.ModernPipeline;

            // Detect platforms
            var platforms = new List<string>();
            if (monoGamePackage?.Attribute("Include")?.Value.Contains("DesktopGL") == true)
                platforms.Add("DesktopGL");
            if (monoGamePackage?.Attribute("Include")?.Value.Contains("WindowsDX") == true)
                platforms.Add("WindowsDX");

            // Get .NET version
            var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value ?? "Unknown";

            // Determine version type
            var versionType = version.Contains("develop") ? VersionType.Development :
                            version.Contains("preview") || version.Contains("rc") || version.Contains("beta") ? VersionType.Preview :
                            VersionType.Stable;

            // Resolve wildcard versions if needed
            if (version.Contains("*"))
            {
                version = await ResolveVersionAsync(version);
            }

            var project = new MonoGameProject
            {
                Name = projectName,
                Path = projectPath,
                Platforms = platforms,
                MonoGameVersion = version,
                VersionType = versionType,
                ProjectType = projectType,
                DotNetVersion = targetFramework,
                LastModified = File.GetLastWriteTimeUtc(projectPath),
                LastScanned = DateTime.UtcNow
            };

            return project;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string> ResolveVersionAsync(string versionPattern)
    {
        try
        {
            // Extract package name from pattern (e.g., "3.8.5-develop.*" -> look for MonoGame.Framework.DesktopGL)
            var resolved = await _nugetService.ResolveWildcardVersionAsync("MonoGame.Framework.DesktopGL", versionPattern);
            return resolved ?? versionPattern;
        }
        catch
        {
            return versionPattern;
        }
    }

    public async Task<bool> BuildProjectAsync(MonoGameProject project, IProgress<string>? progress = null)
    {
        return await ExecuteDotNetCommandAsync("build", project.Path, progress);
    }

    public async Task<bool> RunProjectAsync(MonoGameProject project)
    {
        var projectDir = Path.GetDirectoryName(project.Path);
        if (projectDir == null)
            return false;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = projectDir,
            UseShellExecute = true
        };

        try
        {
            Process.Start(processStartInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestoreProjectAsync(MonoGameProject project, IProgress<string>? progress = null)
    {
        var success = await ExecuteDotNetCommandAsync("restore", project.Path, progress);
        
        // If legacy MGCB, also restore tools
        if (success && project.ProjectType == ProjectType.LegacyMGCB)
        {
            success = await ExecuteDotNetCommandAsync("tool restore", project.Path, progress);
        }

        return success;
    }

    public Task<List<MonoGameProject>> GetAllProjectsAsync()
    {
        return Task.FromResult(_projects.ToList());
    }

    public Task SaveProjectAsync(MonoGameProject project)
    {
        var existing = _projects.FirstOrDefault(p => p.Id == project.Id);
        if (existing != null)
        {
            _projects.Remove(existing);
        }
        _projects.Add(project);
        return Task.CompletedTask;
    }

    public Task RemoveProjectAsync(Guid projectId)
    {
        var project = _projects.FirstOrDefault(p => p.Id == projectId);
        if (project != null)
        {
            _projects.Remove(project);
        }
        return Task.CompletedTask;
    }

    private async Task<bool> ExecuteDotNetCommandAsync(string command, string projectPath, IProgress<string>? progress)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (projectDir == null)
            return false;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = command,
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = processStartInfo };
            
            if (progress != null)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        progress.Report(e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        progress.Report($"ERROR: {e.Data}");
                };
            }

            process.Start();
            
            if (progress != null)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
