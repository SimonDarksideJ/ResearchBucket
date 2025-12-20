using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.App.Services;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    // How long the in-app announcement toast stays visible.
    private static TimeSpan AnnouncementToastDuration => StartupOptions.InAppAnnouncementToastDuration;

    public MainWindowViewModel(
        ProjectsViewModel projects,
        InstallsViewModel installs,
        NewProjectViewModel newProject,
        SettingsViewModel settings,
        BlogViewModel blog,
        ResourcesViewModel resources,
        DocsApiViewModel docsApi,
        OsLauncher os)
    {
        _os = os;

        Projects = projects;
        Installs = installs;
        NewProject = newProject;
        Settings = settings;
        Blog = blog;
        Resources = resources;
        DocsApi = docsApi;

        CloseAnnouncementCommand = new RelayCommand(CloseAnnouncement);
        OpenAnnouncementCommand = new RelayCommand<string>(OpenAnnouncement);

        AppAnnouncementBus.Announcement += OnAnnouncement;

        var (post, thumb) = AppAnnouncementBus.GetSnapshot();
        if (post is not null)
            _ = ShowAnnouncementAsync(post, thumb);
    }

    private readonly OsLauncher _os;
    private CancellationTokenSource? _announcementCts;

    public ProjectsViewModel Projects { get; }
    public InstallsViewModel Installs { get; }
    public NewProjectViewModel NewProject { get; }
    public SettingsViewModel Settings { get; }
    public BlogViewModel Blog { get; }
    public ResourcesViewModel Resources { get; }
    public DocsApiViewModel DocsApi { get; }

    public IRelayCommand CloseAnnouncementCommand { get; }
    public IRelayCommand<string> OpenAnnouncementCommand { get; }

    [ObservableProperty]
    private bool _showAnnouncement;

    [ObservableProperty]
    private double _announcementOpacity;

    [ObservableProperty]
    private BlogPost? _announcementPost;

    [ObservableProperty]
    private IImage? _announcementThumbnail;

    private void OnAnnouncement(BlogPost post, IImage? thumbnail)
    {
        _ = ShowAnnouncementAsync(post, thumbnail);
    }

    private async Task ShowAnnouncementAsync(BlogPost post, IImage? thumbnail)
    {
        _announcementCts?.Cancel();
        _announcementCts?.Dispose();
        _announcementCts = new CancellationTokenSource();
        var ct = _announcementCts.Token;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AnnouncementPost = post;
            AnnouncementThumbnail = thumbnail;
            ShowAnnouncement = true;
            AnnouncementOpacity = 0;
        });

        // Fade in.
        await Dispatcher.UIThread.InvokeAsync(() => AnnouncementOpacity = 1);

        try
        {
            await Task.Delay(AnnouncementToastDuration, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Fade out.
        await Dispatcher.UIThread.InvokeAsync(() => AnnouncementOpacity = 0);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowAnnouncement = false);
    }

    private void CloseAnnouncement()
    {
        _announcementCts?.Cancel();
        _announcementCts?.Dispose();
        _announcementCts = null;

        ShowAnnouncement = false;
        AnnouncementOpacity = 0;
    }

    private void OpenAnnouncement(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _os.OpenUrl(url);
    }
}
