using Avalonia;
using MonoGameHub.App;
using MonoGameHub.App.Services;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		// Debug/testing option:
		//  - ClearCache
		//  - --clearCache
		StartupOptions.ClearCacheRequested = args.Any(a =>
			a.Equals("ClearCache", StringComparison.OrdinalIgnoreCase)
			|| a.Equals("--clearCache", StringComparison.OrdinalIgnoreCase));

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
}
