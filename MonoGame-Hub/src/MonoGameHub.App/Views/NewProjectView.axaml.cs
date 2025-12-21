using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MonoGameHub.App.ViewModels;

namespace MonoGameHub.App.Views;

public sealed partial class NewProjectView : UserControl
{
    public NewProjectView()
    {
        InitializeComponent();
    }

    private async void OnCreateProjectClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewProjectViewModel vm)
            return;

        // If the default projects folder isn't configured yet, prompt for it.
        if (string.IsNullOrWhiteSpace(vm.OutputRoot))
        {
            vm.ProjectsRootMissingWarning = true;
            e.Handled = true;

            var selected = await PickFolderAsync(null, "Select projects folder");
            if (string.IsNullOrWhiteSpace(selected))
                return;

            vm.SetAndPersistProjectsRoot(selected);

            if (vm.CreateProjectCommand.CanExecute(null))
                await vm.CreateProjectCommand.ExecuteAsync(null);
        }
    }

    private async void OnBrowseOutputRootClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewProjectViewModel vm)
            return;

        var selected = await PickFolderAsync(vm.OutputRoot, "Select output root folder");
        if (!string.IsNullOrWhiteSpace(selected))
            vm.OutputRoot = selected;
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
