namespace MonoGameHub.Core.Interfaces;

/// <summary>
/// Service for managing application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads application settings
    /// </summary>
    Task<Models.AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves application settings
    /// </summary>
    Task SaveSettingsAsync(Models.AppSettings settings);

    /// <summary>
    /// Updates the NuGet cache folder environment variable
    /// </summary>
    Task UpdateNuGetCacheFolderAsync(string path);

    /// <summary>
    /// Gets the current .NET SDK version
    /// </summary>
    Task<string?> GetDotNetVersionAsync();
}
