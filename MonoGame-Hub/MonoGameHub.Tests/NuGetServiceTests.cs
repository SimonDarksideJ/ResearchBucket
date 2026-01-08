using MonoGameHub.Core.Services;
using MonoGameHub.Core.Interfaces;

namespace MonoGameHub.Tests;

public class NuGetServiceTests
{
    [Fact]
    public async Task GetPackageVersions_ReturnsVersions()
    {
        // Arrange
        var service = new NuGetService();

        // Act
        var versions = await service.GetPackageVersionsAsync("MonoGame.Framework.DesktopGL", includePrerelease: false);

        // Assert
        Assert.NotEmpty(versions);
    }

    [Fact]
    public async Task GetPackageMetadata_ReturnsMetadata()
    {
        // Arrange
        var service = new NuGetService();

        // Act
        var metadata = await service.GetPackageMetadataAsync("MonoGame.Framework.DesktopGL", "3.8.1.303");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("MonoGame.Framework.DesktopGL", metadata.PackageId);
        Assert.Equal("3.8.1.303", metadata.Version);
    }
}
