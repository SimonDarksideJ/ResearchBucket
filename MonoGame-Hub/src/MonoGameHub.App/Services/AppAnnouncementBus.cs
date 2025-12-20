using Avalonia.Media;
using MonoGameHub.Core.Models;

namespace MonoGameHub.App.Services;

public static class AppAnnouncementBus
{
    private static readonly object Gate = new();

    public static event Action<BlogPost, IImage?>? Announcement;

    public static (BlogPost? Post, IImage? Thumbnail) GetSnapshot()
    {
        lock (Gate)
            return (_post, _thumbnail);
    }

    public static void Publish(BlogPost post, IImage? thumbnail)
    {
        lock (Gate)
        {
            _post = post;
            _thumbnail = thumbnail;
        }

        Announcement?.Invoke(post, thumbnail);
    }

    public static void Clear()
    {
        lock (Gate)
        {
            _post = null;
            _thumbnail = null;
        }
    }

    private static BlogPost? _post;
    private static IImage? _thumbnail;
}
