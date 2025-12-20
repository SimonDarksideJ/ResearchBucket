using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MonoGameHub.App.Converters;

public sealed class PrereleaseBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isPrerelease = value is bool b && b;

        if (Application.Current is { } app &&
            app.Resources.TryGetResource("MonoGameOrange", theme: null, out var orange) &&
            orange is Color)
        {
            var orangeColor = (Color)orange;
            return isPrerelease ? new SolidColorBrush(orangeColor) : Brushes.Black;
        }

        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
