using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia;

namespace MonoGameHub.App.Controls;

internal static class MarkdownInlineBuilder
{
    // Very small inline parser:
    // - Plain text
    // - Markdown links: [text](url)
    // - Inline code: `code`
    // - Emphasis: *em*
    // - Strong: **bold**
    public static IReadOnlyList<Inline> Build(string text, Func<string, bool> onLinkClicked)
    {
        return BuildInternal(text ?? string.Empty, onLinkClicked, depth: 0);
    }

    private static IReadOnlyList<Inline> BuildInternal(string text, Func<string, bool> onLinkClicked, int depth)
    {
        // Prevent pathological recursion on malformed input.
        if (depth > 6)
            return new[] { new Run(text) };

        var inlines = new List<Inline>();
        var buffer = new System.Text.StringBuilder();

        void FlushBuffer()
        {
            if (buffer.Length == 0)
                return;
            inlines.Add(new Run(buffer.ToString()));
            buffer.Clear();
        }

        var i = 0;
        while (i < text.Length)
        {
            var ch = text[i];

            // Escape next char (very small subset; enough for docs content).
            if (ch == '\\' && i + 1 < text.Length)
            {
                buffer.Append(text[i + 1]);
                i += 2;
                continue;
            }

            // Inline code: `code`
            if (ch == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i + 1)
                {
                    FlushBuffer();
                    var code = text[(i + 1)..end];
                    inlines.Add(CreateInlineCode(code));
                    i = end + 1;
                    continue;
                }

                // No closing tick; treat literally.
                buffer.Append(ch);
                i++;
                continue;
            }

            // Link: [text](url)
            if (ch == '[')
            {
                var closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i + 1 && closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket + 2)
                    {
                        FlushBuffer();
                        var linkText = text[(i + 1)..closeBracket];
                        var linkUrl = text[(closeBracket + 2)..closeParen];
                        inlines.Add(CreateLinkInline(linkText, linkUrl, onLinkClicked));
                        i = closeParen + 1;
                        continue;
                    }
                }

                buffer.Append(ch);
                i++;
                continue;
            }

            // Strong/emphasis.
            if (ch == '*')
            {
                // **bold**
                if (i + 1 < text.Length && text[i + 1] == '*')
                {
                    var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                    if (end > i + 2)
                    {
                        FlushBuffer();
                        var inner = text[(i + 2)..end];
                        inlines.Add(CreateStyledSpan(inner, onLinkClicked, depth + 1, bold: true, italic: false));
                        i = end + 2;
                        continue;
                    }
                }

                // *em*
                var endSingle = text.IndexOf('*', i + 1);
                if (endSingle > i + 1)
                {
                    FlushBuffer();
                    var inner = text[(i + 1)..endSingle];
                    inlines.Add(CreateStyledSpan(inner, onLinkClicked, depth + 1, bold: false, italic: true));
                    i = endSingle + 1;
                    continue;
                }

                buffer.Append(ch);
                i++;
                continue;
            }

            buffer.Append(ch);
            i++;
        }

        FlushBuffer();
        return inlines;
    }

    private static void AddRun(List<Inline> inlines, string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        inlines.Add(new Run(value));
    }

    private static Inline CreateStyledSpan(string inner, Func<string, bool> onLinkClicked, int depth, bool bold, bool italic)
    {
        var span = new Span();
        if (bold)
            span.FontWeight = FontWeight.Bold;
        if (italic)
            span.FontStyle = FontStyle.Italic;

        foreach (var inline in BuildInternal(inner, onLinkClicked, depth))
            span.Inlines.Add(inline);

        return span;
    }

    private static Inline CreateInlineCode(string code)
    {
        var text = new TextBlock
        {
            Text = code,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Avalonia.Thickness(0)
        };

        var border = new Border
        {
            Child = text,
            Padding = new Avalonia.Thickness(4, 1),
            Margin = new Avalonia.Thickness(0),
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        if (Application.Current?.TryFindResource("MonoGameHub.Brush.CardBackground", out var bg) == true && bg is IBrush bgBrush)
            border.Background = bgBrush;

        if (Application.Current?.TryFindResource("MonoGameHub.Brush.CardBorder", out var br) == true && br is IBrush borderBrush)
            border.BorderBrush = borderBrush;

        border.BorderThickness = new Avalonia.Thickness(1);

        return new InlineUIContainer { Child = border };
    }

    private static Inline CreateLinkInline(string text, string url, Func<string, bool> onLinkClicked)
    {
        // Use a plain TextBlock so it aligns with the text baseline.
        var link = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(text) ? url : text,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
            Margin = new Avalonia.Thickness(0)
        };

        // Keep styling consistent with the app theme.
        if (Application.Current?.TryFindResource("MonoGameHub.Brush.Accent", out var accent) == true
            && accent is IBrush accentBrush)
        {
            link.Foreground = accentBrush;
        }

        link.PointerPressed += (_, e) =>
        {
            try
            {
                onLinkClicked(url);
                e.Handled = true;
            }
            catch
            {
                // Best-effort; ignore.
            }
        };

        return new InlineUIContainer { Child = link };
    }
}
