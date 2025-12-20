using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace MonoGameHub.App.Controls;

public partial class LogWindow : Window
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<LogWindow, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public LogWindow()
    {
        InitializeComponent();
    }
}
