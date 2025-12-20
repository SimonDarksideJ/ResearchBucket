using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Models;
using MonoGameHub.App.Services;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class BlogViewModel : LoggableViewModel
{
    private readonly MonoGameContentClient _content;
    private readonly RemoteImageLoader _images;
    private readonly OsLauncher _os;
    private IReadOnlyList<BlogPost> _cache = Array.Empty<BlogPost>();
    private bool _isUpdatingTagSelection;

    public BlogViewModel(MonoGameContentClient content, RemoteImageLoader images, OsLauncher os)
    {
        _content = content;
        _images = images;
        _os = os;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        OpenPostCommand = new RelayCommand<string>(OpenPost);

        // Load after startup (best-effort).
        _ = RefreshAsync();
    }

    public ObservableCollection<BlogPostCard> Items { get; } = new();
    public ObservableCollection<TagFilterItemViewModel> TagFilters { get; } = new();

    [ObservableProperty]
    private string _search = string.Empty;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand<string> OpenPostCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            _content.InvalidateCache();
            _cache = await _content.GetBlogPostsAsync(CancellationToken.None);

            RebuildTagFilters();
            ApplyFilter();

            Log($"Loaded {Items.Count} item(s).");
        }
        catch (Exception ex)
        {
            Log($"Blog load failed: {ex.Message}");
        }
    }

    partial void OnSearchChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();

        IEnumerable<BlogPost> filtered = _cache;

        var selectedTag = TagFilters.FirstOrDefault(t => t.IsSelected)?.Name;
        if (!string.IsNullOrWhiteSpace(selectedTag) && !string.Equals(selectedTag, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(p => p.Tags.Any(t => string.Equals(t, selectedTag, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(Search))
            filtered = filtered.Where(i => i.Title.Contains(Search, StringComparison.OrdinalIgnoreCase));

        foreach (var post in filtered.Take(100))
            Items.Add(new BlogPostCard(post));

        _ = LoadThumbnailsAsync(Items.ToArray());
    }

    private void RebuildTagFilters()
    {
        var selected = TagFilters.FirstOrDefault(t => t.IsSelected)?.Name ?? "All";

        var tags = _cache
            .SelectMany(p => p.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        tags.Insert(0, "All");

        _isUpdatingTagSelection = true;
        try
        {
            TagFilters.Clear();

            foreach (var tag in tags)
            {
                var item = new TagFilterItemViewModel(tag)
                {
                    IsSelected = string.Equals(tag, selected, StringComparison.OrdinalIgnoreCase)
                };

                item.PropertyChanged += TagFilterOnPropertyChanged;
                TagFilters.Add(item);
            }

            if (!TagFilters.Any(t => t.IsSelected) && TagFilters.Count > 0)
                TagFilters[0].IsSelected = true;
        }
        finally
        {
            _isUpdatingTagSelection = false;
        }
    }

    private void TagFilterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdatingTagSelection)
            return;

        if (!string.Equals(e.PropertyName, nameof(TagFilterItemViewModel.IsSelected), StringComparison.Ordinal))
            return;

        if (sender is not TagFilterItemViewModel changed)
            return;

        if (!changed.IsSelected)
            return;

        _isUpdatingTagSelection = true;
        try
        {
            foreach (var item in TagFilters)
            {
                if (!ReferenceEquals(item, changed))
                    item.IsSelected = false;
            }
        }
        finally
        {
            _isUpdatingTagSelection = false;
        }

        ApplyFilter();
    }

    private async Task LoadThumbnailsAsync(IReadOnlyList<BlogPostCard> cards)
    {
        foreach (var card in cards)
        {
            if (card.Thumbnail is not null)
                continue;

            var bitmap = await _images.GetAsync(card.Post.ImageUrl, CancellationToken.None);
            if (bitmap is null)
                continue;

            card.Thumbnail = bitmap;
        }
    }

    private void OpenPost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _os.OpenUrl(url);
    }

    public sealed partial class BlogPostCard : ObservableObject
    {
        public BlogPostCard(BlogPost post)
        {
            Post = post;
        }

        public BlogPost Post { get; }

        public string Title => Post.Title;
        public string Author => Post.Author;
        public DateTimeOffset? PublishedAt => Post.PublishedAt;
        public string? PublishedDate => Post.PublishedDate;
        public string Excerpt => Post.Excerpt;
        public string Url => Post.Url;
        public string TagsSummary => Post.Tags.Count == 0 ? string.Empty : string.Join("  ", Post.Tags);

        [ObservableProperty]
        private Bitmap? _thumbnail;
    }
}
