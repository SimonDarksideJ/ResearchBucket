namespace MonoGameHub.Core.Models;

/// <summary>
/// Represents application settings
/// </summary>
public class AppSettings
{
    public string DefaultProjectFolder { get; set; } = 
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MonoGameProjects");
    
    public string NuGetCacheFolder { get; set; } = 
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    public IDEType PreferredIDE { get; set; } = IDEType.SystemDefault;
    public string Language { get; set; } = "en-US";
    public bool CheckForUpdatesOnStartup { get; set; } = true;
}

/// <summary>
/// Application theme mode
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System
}

/// <summary>
/// Preferred IDE for opening projects
/// </summary>
public enum IDEType
{
    VisualStudio,
    VSCode,
    Rider,
    SystemDefault
}
