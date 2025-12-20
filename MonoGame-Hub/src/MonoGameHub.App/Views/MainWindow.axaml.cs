using Avalonia.Controls;
using MonoGameHub.App.ViewModels;

namespace MonoGameHub.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // SelectionChanged won't fire for the initial tab; ensure Projects can populate on startup.
        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                await vm.Projects.OnNavigatedToAsync();
        };
    }

    private async void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        if (tabControl.SelectedItem is not TabItem tab)
            return;

        if (tab.Content is not Control content)
            return;

        if (content.DataContext is ProjectsViewModel projects)
            await projects.OnNavigatedToAsync();

        if (content.DataContext is NewProjectViewModel newProject)
            await newProject.OnNavigatedToAsync();

        if (content.DataContext is InstallsViewModel installs)
            await installs.OnNavigatedToAsync();
    }
}
