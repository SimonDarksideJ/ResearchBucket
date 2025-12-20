using System.Globalization;

namespace MonoGameHub.Core.Models;

public sealed record BlogPost(
    string Title,
    string Url,
    DateTimeOffset? PublishedAt,
    string Author,
    string ImageUrl,
    string Excerpt,
    IReadOnlyList<string> Tags,
    string Markdown,
    string SourceUrl)
{
    public string? PublishedDate => PublishedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
