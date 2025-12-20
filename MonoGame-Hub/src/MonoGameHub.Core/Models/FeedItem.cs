namespace MonoGameHub.Core.Models;

public sealed record FeedItem(
    string Title,
    string Url,
    DateTimeOffset? PublishedAt,
    string? Summary);
