using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

// Legacy compatibility adapter.
// The app's Blog/Resources views now use MonoGameContentClient directly.
public sealed class FeedClient
{
    private readonly MonoGameContentClient _content;

    public FeedClient(MonoGameContentClient content)
    {
        _content = content;
    }

    public async Task<IReadOnlyList<FeedItem>> GetBlogAsync(CancellationToken cancellationToken)
    {
        var posts = await _content.GetBlogPostsAsync(cancellationToken);
        return posts
            .Select(p => new FeedItem(p.Title, p.Url, p.PublishedAt, null))
            .ToList();
    }

    public async Task<IReadOnlyList<FeedItem>> GetResourcesAsync(CancellationToken cancellationToken)
    {
        var resources = await _content.GetResourcesAsync(cancellationToken);
        return resources
            .Select(r => new FeedItem(r.Title, r.Url, null, null))
            .ToList();
    }
}
