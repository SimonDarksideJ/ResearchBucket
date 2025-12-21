using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MonoGameHub.Core.Models;

namespace MonoGameHub.App.ViewModels;

public sealed partial class TocNodeViewModel : ObservableObject
{
    public TocNodeViewModel(string title, Uri? url)
    {
        Title = title;
        Url = url;
        FullTitle = title;
    }

    public string Title { get; }

    [ObservableProperty]
    private Uri? _url;

    // Used for API where leaf title may be a full dotted name.
    [ObservableProperty]
    private string _fullTitle;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<TocNodeViewModel> Children { get; } = new();

    public void SetExpandedRecursive(bool expanded)
    {
        IsExpanded = expanded;
        foreach (var child in Children)
            child.SetExpandedRecursive(expanded);
    }

    public TocNodeViewModel? Filter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return this;

        var contains = Title.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || FullTitle.Contains(filter, StringComparison.OrdinalIgnoreCase);

        var filteredChildren = Children
            .Select(c => c.Filter(filter))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        if (!contains && filteredChildren.Count == 0)
            return null;

        var clone = new TocNodeViewModel(Title, Url)
        {
            FullTitle = FullTitle
        };

        foreach (var child in filteredChildren)
            clone.Children.Add(child);

        return clone;
    }

    public static TocNodeViewModel FromModel(DocsTocNode node)
    {
        var vm = new TocNodeViewModel(node.Title, node.Url)
        {
            FullTitle = node.FullTitle
        };

        foreach (var child in node.Children)
            vm.Children.Add(FromModel(child));

        return vm;
    }
}
