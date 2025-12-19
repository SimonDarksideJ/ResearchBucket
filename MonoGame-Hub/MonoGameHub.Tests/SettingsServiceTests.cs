using MonoGameHub.Core.Services;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task LoadSettings_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        var settings = await service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.NotEmpty(settings.DefaultProjectFolder);
    }

    [Fact]
    public async Task SaveAndLoadSettings_Persists()
    {
        // Arrange
        var service = new SettingsService();
        var originalSettings = await service.LoadSettingsAsync();
        originalSettings.Theme = ThemeMode.Light;

        // Act
        await service.SaveSettingsAsync(originalSettings);
        var loadedSettings = await service.LoadSettingsAsync();

        // Assert
        Assert.Equal(ThemeMode.Light, loadedSettings.Theme);
    }

    [Fact]
    public async Task GetDotNetVersion_ReturnsVersion()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        var version = await service.GetDotNetVersionAsync();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }
}
