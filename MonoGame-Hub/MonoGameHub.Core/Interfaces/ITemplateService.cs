namespace MonoGameHub.Core.Interfaces;

/// <summary>
/// Service for managing MonoGame templates
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Gets available MonoGame templates from NuGet
    /// </summary>
    Task<List<Models.MonoGameTemplate>> GetAvailableTemplatesAsync(bool includePrerelease = false);

    /// <summary>
    /// Gets currently installed templates
    /// </summary>
    Task<List<Models.MonoGameTemplate>> GetInstalledTemplatesAsync();

    /// <summary>
    /// Installs a template version
    /// </summary>
    Task<bool> InstallTemplateAsync(string version, IProgress<string>? progress = null);

    /// <summary>
    /// Uninstalls a template
    /// </summary>
    Task<bool> UninstallTemplateAsync(string packageId);

    /// <summary>
    /// Checks if a specific template version is installed
    /// </summary>
    Task<bool> IsTemplateInstalledAsync(string version);
}
