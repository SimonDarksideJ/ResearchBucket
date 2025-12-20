namespace MonoGameHub.Core.Models;

public sealed record ResourceItem(
    string Title,
    string Author,
    string CoverUrl,
    string Url,
    IReadOnlyList<string> Tags,
    bool PixelArt);
