using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class SplashViewModel : ViewModelBase
{
    // Configure how many log lines are visible on the splash screen.
    // Default is 1 to keep the splash clean and leave room for the logo.
    private const int SplashLogVisibleLines = 1;

    // Approximate per-line height for the splash log area.
    private const double SplashLogLineHeight = 18;
    private const double SplashLogPadding = 20; // top+bottom padding allowance

    public SplashViewModel(MonoGameHub.Core.Services.OsLauncher os)
    {
        _os = os;

        Logo = LoadLogoForCurrentTheme();

        LogPanelHeight = SplashLogPadding + (SplashLogVisibleLines * SplashLogLineHeight);

        foreach (var line in LogBus.GetHistorySnapshot())
            AppendLine(line);

        LogBus.Line += OnLine;

        OpenAnnouncementCommand = new RelayCommand<string>(OpenAnnouncement);
    }

    private readonly MonoGameHub.Core.Services.OsLauncher _os;

    public ObservableCollection<string> LogLines { get; } = new();

    public IImage Logo { get; }

    public double LogPanelHeight { get; }

    public IRelayCommand<string> OpenAnnouncementCommand { get; }

    [ObservableProperty]
    private bool _showAnnouncement;

    [ObservableProperty]
    private double _announcementOpacity;

    [ObservableProperty]
    private BlogPost? _announcementPost;

    public string AnnouncementTag => AnnouncementPost?.Tags?.FirstOrDefault() ?? "NEW";

    [ObservableProperty]
    private IImage? _announcementThumbnail;

    partial void OnAnnouncementPostChanged(BlogPost? value)
    {
        OnPropertyChanged(nameof(AnnouncementTag));
    }

    private static IImage LoadLogoForCurrentTheme()
    {
        var isDark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        var uri = isDark
            ? new Uri("avares://MonoGameHub.App/Assets/Branding/FullColorOnDark/SquareLogo_256px.png")
            : new Uri("avares://MonoGameHub.App/Assets/Branding/FullColorOnLight/SquareLogo_256px.png");

        // AssetLoader.Open never returns null for valid resources; let exceptions surface during development.
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

    private void OnLine(string line)
    {
        void Add() => AppendLine(line);

        if (Dispatcher.UIThread.CheckAccess())
            Add();
        else
            Dispatcher.UIThread.Post(Add);
    }

    private void AppendLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // Keep only the last N lines so the splash stays compact.
        while (LogLines.Count >= SplashLogVisibleLines)
            LogLines.RemoveAt(0);

        LogLines.Add(line);
    }

    public void Dispose()
    {
        LogBus.Line -= OnLine;
    }

    public async Task ShowAnnouncementAsync(BlogPost post, IImage? thumbnail)
    {
        // Splash behavior: fade in immediately and remain visible until the splash window closes.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AnnouncementPost = post;
            AnnouncementThumbnail = thumbnail;

            ShowAnnouncement = true;
            AnnouncementOpacity = 0;

            // Let the Opacity transition run (0 -> 1).
            Dispatcher.UIThread.Post(() => AnnouncementOpacity = 1);
        });
    }

    private void OpenAnnouncement(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _os.OpenUrl(url);
    }
}
