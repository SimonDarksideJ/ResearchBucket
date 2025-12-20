using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MonoGameHub.App.ViewModels;

namespace MonoGameHub.App.Views;

public sealed partial class ProjectsView : UserControl
{
    public ProjectsView()
    {
        InitializeComponent();
    }

    private void OnProjectsListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
            return;

        var project = vm.SelectedProject;
        if (project is null)
            return;

        if (vm.OpenProjectCommand.CanExecute(project))
            vm.OpenProjectCommand.Execute(project);
    }

    private async void OnBrowseProjectsRootClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
            return;

        var selected = await PickFolderAsync(vm.ProjectsRoot, "Select projects folder");
        if (!string.IsNullOrWhiteSpace(selected))
            vm.ProjectsRoot = selected;
    }

    private async Task<string?> PickFolderAsync(string? initialPath, string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;

        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            try
            {
                start = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(initialPath));
            }
            catch
            {
                start = null;
            }
        }

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start
        });

        var folder = result.FirstOrDefault();
        if (folder is null)
            return null;

        return folder.Path.IsFile ? folder.Path.LocalPath : folder.Path.ToString();
    }
}
