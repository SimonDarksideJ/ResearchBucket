using System;
using System.Diagnostics;
using Avalonia.Controls.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MonoGameHub.App.ViewModels;
using MonoGameHub.Core.Models;

namespace MonoGameHub.App.Controls;

public partial class DocsTableView : UserControl
{
    public static readonly StyledProperty<DocsTable?> TableProperty =
        AvaloniaProperty.Register<DocsTableView, DocsTable?>(nameof(Table));

    public DocsTable? Table
    {
        get => GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }

    static DocsTableView()
    {
        TableProperty.Changed.AddClassHandler<DocsTableView>((view, _) => view.Rebuild());
    }

    public DocsTableView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Rebuild()
    {
        var grid = this.FindControl<Grid>("TableGrid")
                   ?? throw new InvalidOperationException("DocsTableView is missing TableGrid.");
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        var table = Table;
        if (table is null)
            return;

        var columnCount = Math.Max(1, table.ColumnCount);
        for (var i = 0; i < columnCount; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        // Header row.
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var col = 0; col < columnCount; col++)
        {
            var text = col < table.Headers.Count ? table.Headers[col] : string.Empty;
            grid.Children.Add(CreateCell(text, isHeader: true, row: 0, col: col));
        }

        // Data rows.
        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];
            var gridRow = rowIndex + 1;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (var col = 0; col < columnCount; col++)
            {
                var text = col < row.Count ? row[col] : string.Empty;
                grid.Children.Add(CreateCell(text, isHeader: false, row: gridRow, col: col));
            }
        }
    }

    private static Control CreateCell(string text, bool isHeader, int row, int col)
    {
        var tb = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontWeight = isHeader ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal,
            Margin = new Thickness(6, 4)
        };

        foreach (var inline in MarkdownInlineBuilder.Build(text, url => HandleLink(tb, url)))
            tb.Inlines!.Add(inline);

        var border = new Border
        {
            Child = tb,
            Padding = new Thickness(0),
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        return border;
    }

    private static bool HandleLink(Control context, string url)
    {
        try
        {
            if (context.DataContext is DocsApiViewModel vm)
            {
                var resolved = vm.ResolveLink(url);
                if (resolved is null)
                    return false;

                if (vm.IsInternalDocsLink(resolved))
                {
                    _ = vm.NavigateToInternalUriAsync(resolved);
                    return true;
                }

                Process.Start(new ProcessStartInfo(resolved.ToString()) { UseShellExecute = true });
                return true;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            {
                Process.Start(new ProcessStartInfo(abs.ToString()) { UseShellExecute = true });
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
