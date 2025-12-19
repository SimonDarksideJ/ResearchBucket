namespace MonoGameHub.Core.Interfaces;

/// <summary>
/// Service for managing MonoGame projects
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Scans a folder for MonoGame projects
    /// </summary>
    Task<List<Models.MonoGameProject>> ScanFolderAsync(string path);

    /// <summary>
    /// Analyzes a specific project to extract metadata
    /// </summary>
    Task<Models.MonoGameProject?> AnalyzeProjectAsync(string projectPath);

    /// <summary>
    /// Resolves a wildcard version pattern to a specific version
    /// </summary>
    Task<string> ResolveVersionAsync(string versionPattern);

    /// <summary>
    /// Builds a MonoGame project
    /// </summary>
    Task<bool> BuildProjectAsync(Models.MonoGameProject project, IProgress<string>? progress = null);

    /// <summary>
    /// Runs a MonoGame project
    /// </summary>
    Task<bool> RunProjectAsync(Models.MonoGameProject project);

    /// <summary>
    /// Restores dependencies for a project
    /// </summary>
    Task<bool> RestoreProjectAsync(Models.MonoGameProject project, IProgress<string>? progress = null);

    /// <summary>
    /// Gets all tracked projects
    /// </summary>
    Task<List<Models.MonoGameProject>> GetAllProjectsAsync();

    /// <summary>
    /// Adds or updates a project in the database
    /// </summary>
    Task SaveProjectAsync(Models.MonoGameProject project);

    /// <summary>
    /// Removes a project from tracking
    /// </summary>
    Task RemoveProjectAsync(Guid projectId);
}
