using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.App.Services;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class ResourcesViewModel : LoggableViewModel
{
    private readonly MonoGameContentClient _content;
    private readonly RemoteImageLoader _images;
    private readonly OsLauncher _os;
    private IReadOnlyList<ResourceItem> _cache = Array.Empty<ResourceItem>();

    public ResourcesViewModel(MonoGameContentClient content, RemoteImageLoader images, OsLauncher os)
    {
        _content = content;
        _images = images;
        _os = os;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        OpenResourceCommand = new RelayCommand<string>(OpenResource);

        // Load after startup (best-effort).
        _ = RefreshAsync();
    }

    public ObservableCollection<ResourceCard> Items { get; } = new();

    public ObservableCollection<TagFilterItemViewModel> TagFilters { get; } = new();

    [ObservableProperty]
    private string _search = string.Empty;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand<string> OpenResourceCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            _content.InvalidateCache();
            _cache = await _content.GetResourcesAsync(CancellationToken.None);

            BuildTagFilters(_cache);
            ApplyFilter();

            Log($"Loaded {Items.Count} item(s).");
        }
        catch (Exception ex)
        {
            Log($"Resources load failed: {ex.Message}");
        }
    }

    partial void OnSearchChanged(string value)
    {
        ApplyFilter();
    }

    private void BuildTagFilters(IReadOnlyList<ResourceItem> resources)
    {
        var selected = TagFilters.Where(t => t.IsSelected).Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        TagFilters.Clear();
        foreach (var tag in resources.SelectMany(r => r.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t))
        {
            var vm = new TagFilterItemViewModel(tag)
            {
                IsSelected = selected.Contains(tag)
            };

            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TagFilterItemViewModel.IsSelected))
                    ApplyFilter();
            };

            TagFilters.Add(vm);
        }
    }

    private void ApplyFilter()
    {
        Items.Clear();

        IEnumerable<ResourceItem> filtered = _cache;

        var selectedTags = TagFilters.Where(t => t.IsSelected).Select(t => t.Name).ToArray();
        if (selectedTags.Length > 0)
        {
            // Simple multi-tag: match ANY selected tag (keeps UX minimal).
            filtered = filtered.Where(r => r.Tags.Any(t => selectedTags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(Search))
            filtered = filtered.Where(i => i.Title.Contains(Search, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered.Take(200))
            Items.Add(new ResourceCard(item));

        _ = LoadCoversAsync(Items.Take(60).ToArray());
    }

    private async Task LoadCoversAsync(IReadOnlyList<ResourceCard> cards)
    {
        foreach (var card in cards)
            await EnsureCoverLoadedAsync(card);
    }

    private async Task EnsureCoverLoadedAsync(ResourceCard card)
    {
        if (card.Cover is not null)
            return;

        var bitmap = await _images.GetAsync(card.Resource.CoverUrl, CancellationToken.None);
        if (bitmap is null)
            return;

        card.Cover = bitmap;
    }

    private void OpenResource(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _os.OpenUrl(url);
    }

    public sealed partial class ResourceCard : ObservableObject
    {
        public ResourceCard(ResourceItem resource)
        {
            Resource = resource;
        }

        public ResourceItem Resource { get; }

        public string Title => Resource.Title;
        public string Description => Resource.Author;
        public IReadOnlyList<string> Tags => Resource.Tags;
        public bool PixelArt => Resource.PixelArt;
        public string Url => Resource.Url;
        public string TagsSummary => Resource.Tags.Count == 0 ? string.Empty : string.Join("  ", Resource.Tags);

        [ObservableProperty]
        private Bitmap? _cover;
    }
}
