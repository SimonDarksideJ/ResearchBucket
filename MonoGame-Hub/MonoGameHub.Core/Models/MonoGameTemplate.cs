namespace MonoGameHub.Core.Models;

/// <summary>
/// Represents a MonoGame template package
/// </summary>
public class MonoGameTemplate
{
    public required string PackageId { get; set; } = "MonoGame.Templates.CSharp";
    public required string Version { get; set; }
    public required string DisplayName { get; set; }
    public VersionType Type { get; set; }
    public DateTime PublishedDate { get; set; }
    public bool IsInstalled { get; set; }
    public string? Description { get; set; }
    public string? DocumentationUrl { get; set; }
    public List<string> AvailableTemplates { get; set; } = new();
}
