using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace MonoGameHub.App.Controls;

public partial class CollapsibleLogPanel : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, string>(nameof(Title), "Logs");

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, bool>(nameof(IsExpanded), defaultValue: false);

    public static readonly StyledProperty<double> CollapsedHeightProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, double>(nameof(CollapsedHeight), defaultValue: 32);

    public static readonly StyledProperty<double> ExpandedHeightProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, double>(nameof(ExpandedHeight), defaultValue: 240);

    public static readonly StyledProperty<double> MinExpandedHeightProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, double>(nameof(MinExpandedHeight), defaultValue: 120);

    public static readonly StyledProperty<double> MaxExpandedHeightProperty =
        AvaloniaProperty.Register<CollapsibleLogPanel, double>(nameof(MaxExpandedHeight), defaultValue: 800);

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public double CollapsedHeight
    {
        get => GetValue(CollapsedHeightProperty);
        set => SetValue(CollapsedHeightProperty, value);
    }

    public double ExpandedHeight
    {
        get => GetValue(ExpandedHeightProperty);
        set => SetValue(ExpandedHeightProperty, value);
    }

    public double MinExpandedHeight
    {
        get => GetValue(MinExpandedHeightProperty);
        set => SetValue(MinExpandedHeightProperty, value);
    }

    public double MaxExpandedHeight
    {
        get => GetValue(MaxExpandedHeightProperty);
        set => SetValue(MaxExpandedHeightProperty, value);
    }

    private LogWindow? _logWindow;
    private INotifyCollectionChanged? _notifyCollection;

    public CollapsibleLogPanel()
    {
        InitializeComponent();

        // Ensure the border height matches initial state.
        UpdateHeight();

        this.GetObservable(IsExpandedProperty).Subscribe(new ActionObserver<bool>(_ => UpdateHeight()));
        this.GetObservable(CollapsedHeightProperty).Subscribe(new ActionObserver<double>(_ => UpdateHeight()));
        this.GetObservable(ExpandedHeightProperty).Subscribe(new ActionObserver<double>(_ => UpdateHeight()));
        this.GetObservable(ItemsSourceProperty).Subscribe(new ActionObserver<IEnumerable?>(_ =>
        {
            UpdateLogWindowItems();
            HookItemsSourceNotifications();
            ScrollToEndIfExpanded();
        }));
        this.GetObservable(TitleProperty).Subscribe(new ActionObserver<string>(_ => UpdateLogWindowTitle()));
    }

    private void HookItemsSourceNotifications()
    {
        if (_notifyCollection is not null)
            _notifyCollection.CollectionChanged -= OnItemsCollectionChanged;

        _notifyCollection = ItemsSource as INotifyCollectionChanged;
        if (_notifyCollection is not null)
            _notifyCollection.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Keep the newest output visible while expanded.
        ScrollToEndIfExpanded();
    }

    private void ScrollToEndIfExpanded()
    {
        if (!IsExpanded)
            return;

        if (ItemsSource is null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (LogListBox is null)
                    return;

                // Scroll to last item.
                if (LogListBox.ItemCount > 0)
                    LogListBox.ScrollIntoView(LogListBox.ItemCount - 1);
            }
            catch
            {
                // Best-effort autoscroll.
            }
        }, DispatcherPriority.Background);
    }

    private sealed class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;

        public ActionObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnNext(T value) => _onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private void OnHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        // Toggle on click anywhere in the bar, except on buttons.
        if (e.Source is Button)
            return;

        IsExpanded = !IsExpanded;
        ScrollToEndIfExpanded();
    }

    private void OnCollapseClicked(object? sender, RoutedEventArgs e)
    {
        IsExpanded = false;
    }

    private void OnResizeDragDelta(object? sender, VectorEventArgs e)
    {
        if (!IsExpanded)
            return;

        // Dragging up increases height (negative Y).
        var proposed = ExpandedHeight - e.Vector.Y;
        ExpandedHeight = Clamp(proposed, MinExpandedHeight, MaxExpandedHeight);
        UpdateHeight();
    }

    private void OnPopOutClicked(object? sender, RoutedEventArgs e)
    {
        if (_logWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var window = new LogWindow
        {
            ItemsSource = ItemsSource,
            Title = Title,
        };

        window.Closed += (_, _) => _logWindow = null;
        _logWindow = window;

        window.Show();
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var clipboard = topLevel?.Clipboard;
        if (clipboard is null)
            return;

        if (ItemsSource is null)
        {
            await clipboard.SetTextAsync(string.Empty);
            return;
        }

        var sb = new StringBuilder();
        foreach (var item in ItemsSource)
        {
            if (item is null)
                continue;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append(item.ToString());
        }

        await clipboard.SetTextAsync(sb.ToString());
    }

    private void UpdateHeight()
    {
        if (RootBorder is null)
            return;

        RootBorder.Height = IsExpanded ? ExpandedHeight : CollapsedHeight;
    }

    private void UpdateLogWindowItems()
    {
        if (_logWindow is not null)
            _logWindow.ItemsSource = ItemsSource;
    }

    private void UpdateLogWindowTitle()
    {
        if (_logWindow is not null)
            _logWindow.Title = Title;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
