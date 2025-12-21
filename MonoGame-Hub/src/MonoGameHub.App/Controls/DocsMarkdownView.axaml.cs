using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using MonoGameHub.App.ViewModels;
using MonoGameHub.Core.Models;

namespace MonoGameHub.App.Controls;

public partial class DocsMarkdownView : UserControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<DocsMarkdownView, string?>(nameof(Markdown));

    private readonly StackPanel _root;

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static DocsMarkdownView()
    {
        MarkdownProperty.Changed.AddClassHandler<DocsMarkdownView>((view, _) => view.Rebuild());
    }

    public DocsMarkdownView()
    {
        InitializeComponent();

        _root = this.FindControl<StackPanel>("Root")
                ?? throw new InvalidOperationException("DocsMarkdownView is missing Root.");

        Rebuild();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Rebuild()
    {
        _root.Children.Clear();

        var markdown = (Markdown ?? string.Empty).Replace("\r\n", "\n");
        if (string.IsNullOrWhiteSpace(markdown))
            return;

        foreach (var block in ParseBlocks(markdown))
        {
            switch (block)
            {
                case HeadingBlock heading:
                    _root.Children.Add(RenderHeading(heading));
                    break;
                case ParagraphBlock paragraph:
                    _root.Children.Add(RenderParagraph(paragraph));
                    break;
                case TableBlock table:
                    _root.Children.Add(new DocsTableView { Table = table.Table });
                    break;
                case ListBlock list:
                    _root.Children.Add(RenderList(list));
                    break;
                case CodeFenceBlock code:
                    _root.Children.Add(RenderCodeFence(code));
                    break;
            }
        }
    }

    private Control RenderHeading(HeadingBlock heading)
    {
        var tb = new TextBlock
        {
            Text = heading.Text,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(0, heading.Level <= 2 ? 8 : 4, 0, 2)
        };

        // Keep it simple: slightly larger headings.
        tb.FontSize = heading.Level switch
        {
            1 => 20,
            2 => 17,
            3 => 15,
            _ => 14
        };

        return tb;
    }

    private Control RenderParagraph(ParagraphBlock paragraph)
    {
        var tb = new TextBlock
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        var inlines = MarkdownInlineBuilder.Build(paragraph.Text, HandleLink);
        foreach (var inline in inlines)
            tb.Inlines!.Add(inline);

        return tb;
    }

    private Control RenderList(ListBlock list)
    {
        var panel = new StackPanel { Spacing = 2 };

        for (var index = 0; index < list.Items.Count; index++)
        {
            var prefix = list.Ordered ? $"{index + 1}." : "•";

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var bullet = new TextBlock
            {
                Text = prefix,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };

            var text = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            foreach (var inline in MarkdownInlineBuilder.Build(list.Items[index], HandleLink))
                text.Inlines!.Add(inline);

            Grid.SetColumn(bullet, 0);
            Grid.SetColumn(text, 1);

            row.Children.Add(bullet);
            row.Children.Add(text);
            panel.Children.Add(row);
        }

        return panel;
    }

    private Control RenderCodeFence(CodeFenceBlock code)
    {
        var textBox = new TextBox
        {
            Text = code.Code,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = textBox
        };

        var border = new Border
        {
            Child = scroll,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 6, 0, 6)
        };
        border.Classes.Add("Card");

        return border;
    }

    private bool HandleLink(string url)
    {
        try
        {
            if (DataContext is DocsApiViewModel vm)
            {
                var resolved = vm.ResolveLink(url);
                if (resolved is null)
                    return false;

                if (vm.IsInternalDocsLink(resolved))
                {
                    _ = vm.NavigateToInternalUriAsync(resolved);
                    return true;
                }

                OpenExternal(resolved);
                return true;
            }

            // Fallback: try open as-is.
            if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            {
                OpenExternal(abs);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void OpenExternal(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        }
        catch
        {
            // Best effort.
        }
    }

    private abstract record Block;
    private sealed record HeadingBlock(int Level, string Text) : Block;
    private sealed record ParagraphBlock(string Text) : Block;
    private sealed record TableBlock(DocsTable Table) : Block;
    private sealed record ListBlock(bool Ordered, IReadOnlyList<string> Items) : Block;
    private sealed record CodeFenceBlock(string Code, string? Language) : Block;

    private static IEnumerable<Block> ParseBlocks(string markdown)
    {
        var lines = markdown.Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Heading: # Title
            if (TryParseHeading(line, out var level, out var headingText))
            {
                yield return new HeadingBlock(level, headingText);
                i++;
                continue;
            }

            // Fenced code block: ```lang
            if (TryParseCodeFenceStart(lines[i], out var language))
            {
                var codeLines = new List<string>();
                i++;
                while (i < lines.Length && !IsCodeFenceEnd(lines[i]))
                {
                    codeLines.Add(lines[i].TrimEnd('\r'));
                    i++;
                }

                if (i < lines.Length && IsCodeFenceEnd(lines[i]))
                    i++;

                yield return new CodeFenceBlock(string.Join("\n", codeLines), language);
                continue;
            }

            // Lists: - item, * item, + item, or 1. item
            if (TryParseList(lines, ref i, out var listBlock))
            {
                yield return listBlock;
                continue;
            }

            // Table: header row + separator row
            if (i + 1 < lines.Length && LooksLikeTableHeader(line) && LooksLikeTableSeparator(lines[i + 1]))
            {
                var headerLine = line;
                var sepLine = lines[i + 1];
                var rowLines = new List<string>();

                i += 2;
                while (i < lines.Length)
                {
                    var rowLine = lines[i].TrimEnd();
                    if (string.IsNullOrWhiteSpace(rowLine))
                        break;

                    if (!LooksLikeTableRow(rowLine))
                        break;

                    rowLines.Add(rowLine);
                    i++;
                }

                var table = ParsePipeTable(headerLine, sepLine, rowLines);
                if (table is not null)
                {
                    yield return new TableBlock(table);
                    continue;
                }

                // If parsing failed, fall back to paragraph.
            }

            // Paragraph: collect until blank line or table/heading.
            var paragraphLines = new List<string>();
            while (i < lines.Length)
            {
                var l = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(l))
                    break;

                // Stop before a new heading.
                if (TryParseHeading(l.TrimStart(), out _, out _))
                    break;

                // Stop before a code fence.
                if (TryParseCodeFenceStart(l, out _))
                    break;

                // Stop before a list.
                if (LooksLikeListItem(l))
                    break;

                // Stop before a table.
                if (i + 1 < lines.Length && LooksLikeTableHeader(l) && LooksLikeTableSeparator(lines[i + 1]))
                    break;

                paragraphLines.Add(l);
                i++;
            }

            if (paragraphLines.Count > 0)
                yield return new ParagraphBlock(string.Join(" ", paragraphLines.Select(x => x.Trim())));
        }
    }

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;

        var trimmed = line.TrimStart();
        var count = 0;
        while (count < trimmed.Length && trimmed[count] == '#')
            count++;

        if (count is < 1 or > 6)
            return false;

        if (count >= trimmed.Length || trimmed[count] != ' ')
            return false;

        level = count;
        text = trimmed[(count + 1)..].Trim();
        return text.Length > 0;
    }

    private static bool TryParseCodeFenceStart(string line, out string? language)
    {
        language = null;

        var t = (line ?? string.Empty).TrimEnd();
        if (!t.StartsWith("```", StringComparison.Ordinal))
            return false;

        var suffix = t[3..].Trim();
        if (suffix.Length > 0)
            language = suffix;

        return true;
    }

    private static bool IsCodeFenceEnd(string line)
        => (line ?? string.Empty).TrimEnd().StartsWith("```", StringComparison.Ordinal);

    private static bool LooksLikeListItem(string line)
    {
        var t = (line ?? string.Empty).TrimStart();
        if (t.StartsWith("- ") || t.StartsWith("* ") || t.StartsWith("+ "))
            return true;

        var dotIndex = t.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0 && dotIndex + 1 < t.Length && t[dotIndex + 1] == ' ')
        {
            for (var j = 0; j < dotIndex; j++)
            {
                if (!char.IsDigit(t[j]))
                    return false;
            }

            return true;
        }

        return false;
    }

    private static bool TryParseList(string[] lines, ref int index, out ListBlock list)
    {
        list = null!;

        if (index >= lines.Length)
            return false;

        if (!LooksLikeListItem(lines[index]))
            return false;

        var ordered = IsOrderedListItem(lines[index]);
        var items = new List<string>();

        while (index < lines.Length)
        {
            var raw = lines[index].TrimEnd();
            if (string.IsNullOrWhiteSpace(raw))
                break;

            if (!LooksLikeListItem(raw) || IsOrderedListItem(raw) != ordered)
                break;

            items.Add(StripListPrefix(raw));
            index++;
        }

        if (items.Count == 0)
            return false;

        list = new ListBlock(ordered, items);
        return true;
    }

    private static bool IsOrderedListItem(string line)
    {
        var t = (line ?? string.Empty).TrimStart();
        var dotIndex = t.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0)
            return false;

        if (dotIndex + 1 >= t.Length || t[dotIndex + 1] != ' ')
            return false;

        for (var j = 0; j < dotIndex; j++)
        {
            if (!char.IsDigit(t[j]))
                return false;
        }

        return true;
    }

    private static string StripListPrefix(string line)
    {
        var t = (line ?? string.Empty).TrimStart();
        if (t.StartsWith("- ") || t.StartsWith("* ") || t.StartsWith("+ "))
            return t[2..].Trim();

        var dotIndex = t.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0 && dotIndex + 1 < t.Length && t[dotIndex + 1] == ' ')
            return t[(dotIndex + 2)..].Trim();

        return t.Trim();
    }

    private static bool LooksLikeTableHeader(string line)
        => line.Contains('|') && line.TrimStart().StartsWith('|');

    private static bool LooksLikeTableRow(string line)
        => line.Contains('|') && line.TrimStart().StartsWith('|');

    private static bool LooksLikeTableSeparator(string line)
    {
        var t = line.Trim();
        if (!t.Contains('|'))
            return false;

        // Typical: | --- | ---: |
        foreach (var cell in SplitPipeCells(t))
        {
            var c = cell.Trim();
            if (c.Length == 0)
                continue;
            c = c.Trim(':');
            if (c.Length < 3)
                return false;
            if (c.Any(ch => ch != '-'))
                return false;
        }

        return true;
    }

    private static DocsTable? ParsePipeTable(string headerLine, string _separatorLine, IReadOnlyList<string> rowLines)
    {
        var headers = SplitPipeCells(headerLine).Select(UnescapeCell).Select(x => x.Trim()).ToList();
        if (headers.Count == 0)
            return null;

        var rows = new List<IReadOnlyList<string>>();
        foreach (var rowLine in rowLines)
        {
            var cells = SplitPipeCells(rowLine).Select(UnescapeCell).Select(x => x.Trim()).ToList();
            if (cells.Count == 0)
                continue;
            rows.Add(cells);
        }

        return new DocsTable(headers, rows);
    }

    private static IEnumerable<string> SplitPipeCells(string line)
    {
        // Expect leading/trailing pipes, but be tolerant.
        var t = line.Trim();
        if (t.StartsWith('|'))
            t = t[1..];
        if (t.EndsWith('|'))
            t = t[..^1];

        var current = new System.Text.StringBuilder();
        var escape = false;
        foreach (var ch in t)
        {
            if (escape)
            {
                current.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '|')
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        yield return current.ToString();
    }

    private static string UnescapeCell(string cell)
        => cell.Replace("\\|", "|");
}
