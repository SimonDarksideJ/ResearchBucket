namespace MonoGameHub.Core.Interfaces;

/// <summary>
/// Service for interacting with NuGet
/// </summary>
public interface INuGetService
{
    /// <summary>
    /// Gets all versions of a package
    /// </summary>
    Task<List<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false);

    /// <summary>
    /// Resolves a wildcard version pattern to the latest matching version
    /// </summary>
    Task<string?> ResolveWildcardVersionAsync(string packageId, string pattern);

    /// <summary>
    /// Gets metadata for a specific package version
    /// </summary>
    Task<PackageMetadata?> GetPackageMetadataAsync(string packageId, string version);
}

/// <summary>
/// Package metadata information
/// </summary>
public class PackageMetadata
{
    public required string PackageId { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public DateTime? Published { get; set; }
    public string? Authors { get; set; }
}
