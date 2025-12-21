using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGameHub.Core.Models;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public sealed partial class DocsApiViewModel : ViewModelBase
{
    private readonly DocsSiteCache _docs;
    private int _tocReloadVersion;
    private int _pageLoadVersion;
    private bool _suppressTocSelectionLoad;

    public DocsApiViewModel(DocsSiteCache docs)
    {
        _docs = docs;

        ClearFilterCommand = new RelayCommand(() => FilterText = string.Empty);

        // Default to Docs mode.
        SelectedMode = DocsSiteMode.Docs;

        // Load after startup (best-effort).
        _ = ReloadTocAsync();
    }

    public ObservableCollection<TocNodeViewModel> TocItems { get; } = new();

    public IRelayCommand ClearFilterCommand { get; }

    [ObservableProperty]
    private DocsSiteMode _selectedMode;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private TocNodeViewModel? _selectedTocItem;

    [ObservableProperty]
    private string _selectedPageMarkdown = string.Empty;

    [ObservableProperty]
    private string? _currentPageUrl;

    public bool ShowLogo => string.IsNullOrWhiteSpace(SelectedPageMarkdown);
    public bool ShowContent => !ShowLogo;
    public bool ShowClearFilter => !string.IsNullOrWhiteSpace(FilterText);

    public bool IsDocsMode
    {
        get => SelectedMode == DocsSiteMode.Docs;
        set
        {
            if (value)
                SelectedMode = DocsSiteMode.Docs;
        }
    }

    public bool IsApiMode
    {
        get => SelectedMode == DocsSiteMode.Api;
        set
        {
            if (value)
                SelectedMode = DocsSiteMode.Api;
        }
    }

    partial void OnSelectedModeChanged(DocsSiteMode value)
    {
        OnPropertyChanged(nameof(IsDocsMode));
        OnPropertyChanged(nameof(IsApiMode));

        FilterText = string.Empty;
        SelectedTocItem = null;
        SelectedPageMarkdown = string.Empty;
        OnPropertyChanged(nameof(ShowLogo));
        OnPropertyChanged(nameof(ShowContent));

        _ = ReloadTocAsync();
    }

    partial void OnFilterTextChanged(string value)
    {
        OnPropertyChanged(nameof(ShowClearFilter));
        _ = ReloadTocAsync();
    }

    partial void OnSelectedTocItemChanged(TocNodeViewModel? value)
    {
        if (_suppressTocSelectionLoad)
            return;

        _ = LoadSelectedPageAsync(value);
    }

    partial void OnSelectedPageMarkdownChanged(string value)
    {
        OnPropertyChanged(nameof(ShowLogo));
        OnPropertyChanged(nameof(ShowContent));
    }

    private async Task ReloadTocAsync()
    {
        var version = Interlocked.Increment(ref _tocReloadVersion);

        var tocModels = await _docs.GetTocAsync(SelectedMode, CancellationToken.None);

        var toc = tocModels
            .Select(TocNodeViewModel.FromModel)
            .ToList();

        if (version != Volatile.Read(ref _tocReloadVersion))
            return;

        // Apply filter.
        var filter = (FilterText ?? string.Empty).Trim();
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? toc
            : toc
                .Select(n => n.Filter(filter))
                .Where(n => n is not null)
                .Select(n => n!)
                .ToList();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            foreach (var node in filtered)
                node.SetExpandedRecursive(true);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Avoid binding/collection cross-thread issues.
            TocItems.Clear();
            foreach (var node in filtered)
                TocItems.Add(node);
        });
    }

    private async Task LoadSelectedPageAsync(TocNodeViewModel? node)
    {
        var version = Interlocked.Increment(ref _pageLoadVersion);

        if (node?.Url is null)
        {
            // Only clear if this is still the latest request.
            if (version != Volatile.Read(ref _pageLoadVersion))
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (version != Volatile.Read(ref _pageLoadVersion))
                    return;

                CurrentPageUrl = null;
                SelectedPageMarkdown = string.Empty;
            });
            return;
        }

        var markdown = await _docs.GetPageMarkdownAsync(SelectedMode, node.Url, CancellationToken.None);

        if (version != Volatile.Read(ref _pageLoadVersion))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentPageUrl = node.Url.ToString();
            SelectedPageMarkdown = markdown;
        });
    }

    public Uri? ResolveLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Ignore pure anchors for now (no in-document scrolling support).
        if (url.StartsWith('#'))
            return null;

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return absolute;

        if (!string.IsNullOrWhiteSpace(CurrentPageUrl)
            && Uri.TryCreate(CurrentPageUrl, UriKind.Absolute, out var baseUri)
            && Uri.TryCreate(baseUri, url, out var resolved))
        {
            return resolved;
        }

        return null;
    }

    public bool IsInternalDocsLink(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            return false;

        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return false;

        return uri.Host.Equals("docs.monogame.net", StringComparison.OrdinalIgnoreCase);
    }

    public async Task NavigateToInternalUriAsync(Uri uri)
    {
        if (!IsInternalDocsLink(uri))
            return;

        var mode = GuessModeFromUri(uri) ?? SelectedMode;
        if (SelectedMode != mode)
            SelectedMode = mode;

        // Load the page directly (don’t require it to exist in the current TOC selection).
        var version = Interlocked.Increment(ref _pageLoadVersion);
        var markdown = await _docs.GetPageMarkdownAsync(mode, uri, CancellationToken.None);

        if (version != Volatile.Read(ref _pageLoadVersion))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Clearing SelectedTocItem normally triggers LoadSelectedPageAsync(null) which clears the page.
            // Suppress that side-effect during internal navigation.
            _suppressTocSelectionLoad = true;
            SelectedTocItem = null;
            _suppressTocSelectionLoad = false;
            CurrentPageUrl = uri.ToString();
            SelectedPageMarkdown = markdown;
        });
    }

    private static DocsSiteMode? GuessModeFromUri(Uri uri)
    {
        var path = uri.AbsolutePath ?? string.Empty;
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return DocsSiteMode.Api;
        if (path.StartsWith("/articles/", StringComparison.OrdinalIgnoreCase))
            return DocsSiteMode.Docs;

        return null;
    }
}

