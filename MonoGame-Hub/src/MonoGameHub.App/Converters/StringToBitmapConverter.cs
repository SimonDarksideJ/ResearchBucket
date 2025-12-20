using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MonoGameHub.App.Converters;

public sealed class StringToBitmapConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Bitmap bitmap)
            return bitmap;

        if (value is not string s)
            return null;

        var uriString = s.Trim();
        if (uriString.Length == 0)
            return null;

        return Cache.GetOrAdd(uriString, static key =>
        {
            try
            {
                var uri = new Uri(key, UriKind.Absolute);
                using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
