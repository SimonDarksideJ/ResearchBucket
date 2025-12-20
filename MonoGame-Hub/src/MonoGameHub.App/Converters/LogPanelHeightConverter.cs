using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MonoGameHub.App.Converters;

public sealed class LogPanelHeightConverter : IValueConverter
{
    public double CollapsedHeight { get; set; } = 32;
    public double ExpandedHeight { get; set; } = 200;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? ExpandedHeight : CollapsedHeight;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
