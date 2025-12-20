using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

public sealed class MonoGameContentClient
{
    private const string BlogDirectoryApiUrl = "https://api.github.com/repos/MonoGame/monogame.github.io/contents/website/content/blog?ref=main";
    private const string ResourcesJsonUrl = "https://raw.githubusercontent.com/MonoGame/monogame.github.io/main/website/_data/resources.json";
    private const string BlogBaseUrl = "https://monogame.net/blog/";
    private static readonly Uri MonoGameSiteBaseUri = new("https://monogame.net/");

    private readonly HttpClient _http;
    private IReadOnlyList<BlogPost>? _blogCache;
    private IReadOnlyList<ResourceItem>? _resourcesCache;

    public MonoGameContentClient(HttpClient http)
    {
        _http = http;
    }

    public void InvalidateCache()
    {
        _blogCache = null;
        _resourcesCache = null;
    }

    public async Task<IReadOnlyList<BlogPost>> GetBlogPostsAsync(CancellationToken cancellationToken)
    {
        if (_blogCache is not null)
            return _blogCache;

        using var request = new HttpRequestMessage(HttpMethod.Get, BlogDirectoryApiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MonoGameHub", "1.0"));

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var items = JsonSerializer.Deserialize<List<GitHubContentItem>>(json, JsonOptions) ?? new();

        var posts = new List<BlogPost>();

        foreach (var item in items)
        {
            if (!string.Equals(item.Type, "file", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!item.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(item.DownloadUrl))
                continue;

            var fileContent = await _http.GetStringAsync(item.DownloadUrl, cancellationToken);
            var (frontMatter, markdownBody) = SplitFrontMatter(fileContent);

            var slug = Path.GetFileNameWithoutExtension(item.Name);
            var title = GetFrontMatterValue(frontMatter, "title")
                        ?? HumanizeSlug(slug);

            var author = GetFrontMatterValue(frontMatter, "author") ?? string.Empty;
            var imageUrl = ToAbsoluteMonoGameUrl(GetFrontMatterValue(frontMatter, "image") ?? string.Empty);
            var excerpt = GetFrontMatterValue(frontMatter, "excerpt")
                          ?? CreateExcerptFromMarkdown(markdownBody);

            var tags = GetFrontMatterTags(frontMatter, "tags");

            var publishedAt = ParseFrontMatterDate(frontMatter)
                              ?? ParseLeadingFileDate(slug);

            var url = new Uri(new Uri(BlogBaseUrl), $"{slug}/").ToString();

            posts.Add(new BlogPost(
                Title: title,
                Url: url,
                PublishedAt: publishedAt,
                Author: author,
                ImageUrl: imageUrl,
                Excerpt: excerpt,
                Tags: tags,
                Markdown: markdownBody,
                SourceUrl: item.DownloadUrl));
        }

        _blogCache = posts
            .OrderByDescending(p => p.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(p => p.Title)
            .ToList();

        return _blogCache;
    }

    public async Task<IReadOnlyList<ResourceItem>> GetResourcesAsync(CancellationToken cancellationToken)
    {
        if (_resourcesCache is not null)
            return _resourcesCache;

        var json = await _http.GetStringAsync(ResourcesJsonUrl, cancellationToken);
        var resources = JsonSerializer.Deserialize<List<ResourceJson>>(json, JsonOptions) ?? new();

        _resourcesCache = resources
            .Where(r => !string.IsNullOrWhiteSpace(r.Title) && !string.IsNullOrWhiteSpace(r.Url))
            .Select(r => new ResourceItem(
                Title: r.Title!.Trim(),
                Author: (r.Author ?? string.Empty).Trim(),
                CoverUrl: ToAbsoluteMonoGameUrl((r.Cover ?? string.Empty).Trim()),
                Url: r.Url!.Trim(),
                Tags: (r.Tags ?? Array.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray(),
                PixelArt: r.PixelArt ?? false))
            .OrderBy(r => r.Title)
            .ToList();

        return _resourcesCache;
    }

    private static (string frontMatter, string body) SplitFrontMatter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, string.Empty);

        var normalized = text.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return (string.Empty, normalized);

        var end = normalized.IndexOf("\n---\n", "---\n".Length, StringComparison.Ordinal);
        if (end < 0)
            return (string.Empty, normalized);

        var frontMatter = normalized.Substring("---\n".Length, end - "---\n".Length);
        var body = normalized.Substring(end + "\n---\n".Length);
        return (frontMatter.Trim(), body.Trim());
    }

    private static string? GetFrontMatterValue(string frontMatter, string key)
    {
        if (string.IsNullOrWhiteSpace(frontMatter))
            return null;

        foreach (var rawLine in frontMatter.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var idx = line.IndexOf(':');
            if (idx <= 0)
                continue;

            var k = line[..idx].Trim();
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                continue;

            var v = line[(idx + 1)..].Trim();
            if (v.Length == 0)
                return null;

            // Remove wrapping quotes if present.
            if ((v.StartsWith('"') && v.EndsWith('"')) || (v.StartsWith('\'') && v.EndsWith('\'')))
                v = v[1..^1];

            return v.Trim();
        }

        return null;
    }

    private static IReadOnlyList<string> GetFrontMatterTags(string frontMatter, string key)
    {
        if (string.IsNullOrWhiteSpace(frontMatter))
            return Array.Empty<string>();

        var lines = frontMatter.Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var idx = line.IndexOf(':');
            if (idx <= 0)
                continue;

            var k = line[..idx].Trim();
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = line[(idx + 1)..].Trim();

            // Inline forms:
            // tags: releases
            // tags: [foundation]
            // tags: tag1, tag2
            if (remainder.Length > 0)
                return ParseTagValue(remainder);

            // Multiline YAML list:
            // tags:
            //  - foo
            //  - bar
            var tags = new List<string>();
            for (var j = i + 1; j < lines.Length; j++)
            {
                var next = lines[j];
                if (string.IsNullOrWhiteSpace(next))
                    break;

                var trimmed = next.Trim();
                if (!trimmed.StartsWith('-'))
                    break;

                var t = trimmed[1..].Trim();
                if (t.Length == 0)
                    continue;

                if ((t.StartsWith('"') && t.EndsWith('"')) || (t.StartsWith('\'') && t.EndsWith('\'')))
                    t = t[1..^1];

                tags.Add(t);
            }

            return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseTagValue(string raw)
    {
        var v = raw.Trim();
        if ((v.StartsWith('"') && v.EndsWith('"')) || (v.StartsWith('\'') && v.EndsWith('\'')))
            v = v[1..^1];

        if (v.StartsWith('[') && v.EndsWith(']'))
        {
            v = v[1..^1];
            return v
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().Trim('"', '\''))
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (v.Contains(','))
        {
            return v
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().Trim('"', '\''))
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return string.IsNullOrWhiteSpace(v)
            ? Array.Empty<string>()
            : new[] { v };
    }

    private static string ToAbsoluteMonoGameUrl(string url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (trimmed.StartsWith('/'))
            return new Uri(MonoGameSiteBaseUri, trimmed).ToString();

        return trimmed;
    }

    private static DateTimeOffset? ParseFrontMatterDate(string frontMatter)
    {
        var raw = GetFrontMatterValue(frontMatter, "date");
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return new DateTimeOffset(dt);

        return null;
    }

    private static DateTimeOffset? ParseLeadingFileDate(string slug)
    {
        // Many posts use yyyy-MM-dd-... naming.
        if (slug.Length < 10)
            return null;

        var prefix = slug[..10];
        if (!DateTime.TryParseExact(prefix, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return null;

        return new DateTimeOffset(dt, TimeSpan.Zero);
    }

    private static string HumanizeSlug(string slug)
    {
        var withoutDate = slug;
        if (slug.Length > 11 && slug[4] == '-' && slug[7] == '-' && slug[10] == '-')
            withoutDate = slug[11..];

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(withoutDate.Replace('-', ' '));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record GitHubContentItem(
        string Name,
        string Type,
        [property: JsonPropertyName("download_url")] string? DownloadUrl);

    private sealed record ResourceJson(
        string? Title,
        string? Author,
        string? Cover,
        string? Url,
        string[]? Tags,
        bool? PixelArt);

    private static string CreateExcerptFromMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Very lightweight: take the first non-empty non-heading line and strip a few common markdown tokens.
        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith('#'))
                continue;

            // Skip image-only lines.
            if (line.StartsWith("![", StringComparison.Ordinal))
                continue;

            var cleaned = line
                .Replace("**", "")
                .Replace("*", "")
                .Replace("`", "")
                .Replace("_", "");

            // Roughly strip markdown links: [text](url) -> text
            cleaned = StripMarkdownLinks(cleaned);

            if (cleaned.Length > 220)
                cleaned = cleaned[..220].TrimEnd() + "…";

            return cleaned;
        }

        return string.Empty;
    }

    private static string StripMarkdownLinks(string text)
    {
        // Not a full parser; handles common inline links.
        // Example: "See [MonoGame](https://monogame.net)" -> "See MonoGame"
        var result = text;
        while (true)
        {
            var openBracket = result.IndexOf('[', StringComparison.Ordinal);
            if (openBracket < 0)
                break;

            var closeBracket = result.IndexOf(']', openBracket + 1);
            if (closeBracket < 0)
                break;

            if (closeBracket + 1 >= result.Length || result[closeBracket + 1] != '(')
            {
                result = result[(closeBracket + 1)..];
                continue;
            }

            var closeParen = result.IndexOf(')', closeBracket + 2);
            if (closeParen < 0)
                break;

            var linkText = result.Substring(openBracket + 1, closeBracket - openBracket - 1);
            result = result.Substring(0, openBracket) + linkText + result[(closeParen + 1)..];
        }

        return result;
    }
}
