using System.Diagnostics;
using System.Text.RegularExpressions;
using MonoGameHub.Core.Interfaces;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

/// <summary>
/// Service for managing MonoGame templates
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly INuGetService _nugetService;

    public TemplateService(INuGetService nugetService)
    {
        _nugetService = nugetService;
    }

    public async Task<List<MonoGameTemplate>> GetAvailableTemplatesAsync(bool includePrerelease = false)
    {
        var templates = new List<MonoGameTemplate>();

        try
        {
            var versions = await _nugetService.GetPackageVersionsAsync("MonoGame.Templates.CSharp", includePrerelease);
            
            foreach (var version in versions)
            {
                var metadata = await _nugetService.GetPackageMetadataAsync("MonoGame.Templates.CSharp", version);
                
                var versionType = version.Contains("develop") ? VersionType.Development :
                                version.Contains("preview") || version.Contains("rc") || version.Contains("beta") ? VersionType.Preview :
                                VersionType.Stable;

                var template = new MonoGameTemplate
                {
                    PackageId = "MonoGame.Templates.CSharp",
                    Version = version,
                    DisplayName = $"MonoGame Templates {version}",
                    Type = versionType,
                    PublishedDate = metadata?.Published ?? DateTime.MinValue,
                    IsInstalled = false,
                    Description = metadata?.Description,
                    DocumentationUrl = "https://docs.monogame.net/"
                };

                templates.Add(template);
            }
        }
        catch
        {
            // Return empty list on error
        }

        return templates;
    }

    public async Task<List<MonoGameTemplate>> GetInstalledTemplatesAsync()
    {
        var templates = new List<MonoGameTemplate>();

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output to find MonoGame templates
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("MonoGame", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract template information from the line
                    // This is a simplified version - real implementation would need more robust parsing
                    var match = Regex.Match(line, @"MonoGame.*?(\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        templates.Add(new MonoGameTemplate
                        {
                            PackageId = "MonoGame.Templates.CSharp",
                            Version = version,
                            DisplayName = $"MonoGame Templates {version}",
                            Type = VersionType.Stable,
                            PublishedDate = DateTime.MinValue,
                            IsInstalled = true
                        });
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return templates;
    }

    public async Task<bool> InstallTemplateAsync(string version, IProgress<string>? progress = null)
    {
        try
        {
            // First, uninstall any existing templates
            await UninstallTemplateAsync("MonoGame.Templates.CSharp");

            // Install the new version
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new install MonoGame.Templates.CSharp::{version}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

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

    public async Task<bool> UninstallTemplateAsync(string packageId)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new uninstall {packageId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            await process.WaitForExitAsync();
            
            // Exit code 0 means success, but also accept non-zero if template wasn't installed
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsTemplateInstalledAsync(string version)
    {
        var installed = await GetInstalledTemplatesAsync();
        return installed.Any(t => t.Version == version);
    }
}
