using Avalonia.Controls;
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

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is SplashViewModel vm)
            vm.Dispose();
    }
}
