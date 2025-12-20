using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGameHub.App.ViewModels;

public sealed partial class TagFilterItemViewModel : ObservableObject
{
    public TagFilterItemViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
}
