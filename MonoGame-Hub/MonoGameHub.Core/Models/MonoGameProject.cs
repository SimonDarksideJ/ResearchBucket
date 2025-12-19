namespace MonoGameHub.Core.Models;

/// <summary>
/// Represents a MonoGame project
/// </summary>
public class MonoGameProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Path { get; set; }
    public List<string> Platforms { get; set; } = new();
    public required string MonoGameVersion { get; set; }
    public VersionType VersionType { get; set; }
    public ProjectType ProjectType { get; set; }
    public required string DotNetVersion { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime LastScanned { get; set; } = DateTime.UtcNow;
    public bool IsFavorite { get; set; }
}

/// <summary>
/// Type of MonoGame version (stable, preview, or development)
/// </summary>
public enum VersionType
{
    Stable,
    Preview,
    Development
}

/// <summary>
/// Type of MonoGame project (legacy MGCB or modern pipeline)
/// </summary>
public enum ProjectType
{
    LegacyMGCB,
    ModernPipeline
}
