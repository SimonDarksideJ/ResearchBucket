using System.Diagnostics;
using System.Text.Json;
using MonoGameHub.Core.Interfaces;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

/// <summary>
/// Service for managing application settings
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "MonoGameHub");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return default settings
        }

        return new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Silently fail if settings can't be saved
        }
    }

    public Task UpdateNuGetCacheFolderAsync(string path)
    {
        try
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", path, EnvironmentVariableTarget.User);
            return Task.CompletedTask;
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    public async Task<string?> GetDotNetVersionAsync()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            
            var version = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return version.Trim();
        }
        catch
        {
            return null;
        }
    }
}
