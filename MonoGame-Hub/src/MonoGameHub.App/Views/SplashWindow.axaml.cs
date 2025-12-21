using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MonoGameHub.App.ViewModels;

namespace MonoGameHub.App.Views;

public sealed partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnDragAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Don't start dragging if the press originated from the close button.
        var sourceControl = e.Source as Control;
        while (sourceControl is not null)
        {
            if (sourceControl is Button)
                return;

            sourceControl = sourceControl.Parent as Control;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        BeginMoveDrag(e);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is SplashViewModel vm)
            vm.Dispose();
    }
}
