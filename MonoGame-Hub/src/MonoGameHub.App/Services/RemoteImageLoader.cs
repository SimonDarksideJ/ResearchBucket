using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Media.Imaging;

namespace MonoGameHub.App.Services;

public sealed class RemoteImageLoader
{
    private const string DefaultLightUrl = "https://raw.githubusercontent.com/MonoGame/MonoGame.Logo/master/FullColorOnLight/SquareLogo_512px.png";
    private const string DefaultDarkUrl = "https://raw.githubusercontent.com/MonoGame/MonoGame.Logo/master/FullColorOnDark/SquareLogo_512px.png";

    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, Lazy<Task<Bitmap?>>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public RemoteImageLoader(HttpClient http)
    {
        _http = http;
    }

    public Task<Bitmap?> GetAsync(string? url, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(url)
            ? GetDefaultUrlForCurrentTheme()
            : url.Trim();

        var lazy = _cache.GetOrAdd(key, k => new Lazy<Task<Bitmap?>>(() => DownloadWithFallbackAsync(k, cancellationToken)));
        return lazy.Value;
    }

    private Task<Bitmap?> DownloadWithFallbackAsync(string url, CancellationToken cancellationToken)
        => DownloadAsync(url, cancellationToken)
            .ContinueWith(async t =>
            {
                var bitmap = t.Status == TaskStatus.RanToCompletion ? t.Result : null;
                if (bitmap is not null)
                    return bitmap;

                var fallbackUrl = GetDefaultUrlForCurrentTheme();
                if (string.Equals(url, fallbackUrl, StringComparison.OrdinalIgnoreCase))
                    return null;

                // Go through cache for fallback too.
                return await GetAsync(fallbackUrl, cancellationToken);
            }, cancellationToken)
            .Unwrap();

    private async Task<Bitmap?> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await _http.GetStreamAsync(url, cancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);

            if (!LooksLikeImage(ms))
                return null;

            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeImage(MemoryStream ms)
    {
        // Best-effort signature check to avoid throwing inside Bitmap for obviously invalid payloads.
        if (ms.Length < 12)
            return false;

        var bytes = ms.GetBuffer();

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return true;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;

        // GIF: "GIF87a" or "GIF89a"
        if (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'8' &&
            (bytes[4] == (byte)'7' || bytes[4] == (byte)'9') && bytes[5] == (byte)'a')
            return true;

        // WEBP: RIFF....WEBP
        if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return true;

        return false;
    }

    private static string GetDefaultUrlForCurrentTheme()
    {
        // Best-effort: fall back to light if theme is unknown.
        var theme = Application.Current?.ActualThemeVariant;
        return theme == ThemeVariant.Dark ? DefaultDarkUrl : DefaultLightUrl;
    }
}
