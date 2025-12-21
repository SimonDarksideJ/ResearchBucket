using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using MonoGameHub.Core.Models;

namespace MonoGameHub.Core.Services;

public sealed class DocsSiteCache
{
    private static readonly Uri DocsIndexUri = new("https://docs.monogame.net/articles/index.html");
    private static readonly Uri ApiIndexUri = new("https://docs.monogame.net/api/index.html");

    private readonly HttpClient _http;
    private readonly string _cacheRoot;

    public DocsSiteCache(HttpClient http)
    {
        _http = http;

        _cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonoGameHub",
            "cache",
            "docs-site");

        Directory.CreateDirectory(_cacheRoot);
    }

    public async Task<IReadOnlyList<DocsTocNode>> GetTocAsync(DocsSiteMode mode, CancellationToken cancellationToken)
    {
        var tocPath = Path.Combine(_cacheRoot, mode == DocsSiteMode.Docs ? "toc-docs.json" : "toc-api.json");

        // If online, refresh; if offline, fall back to cache.
        var html = await TryDownloadStringAsync(mode == DocsSiteMode.Docs ? DocsIndexUri : ApiIndexUri, cancellationToken);
        if (html is null)
        {
            if (File.Exists(tocPath))
                return LoadTocFromDisk(tocPath);

            return Array.Empty<DocsTocNode>();
        }

        IReadOnlyList<DocsTocNode> toc;

        if (mode == DocsSiteMode.Docs)
        {
            // DocFX pages render the actual navigation tree from /articles/toc.html.
            // Parse that to match the website's left navigation structure.
            var tocUri = new Uri(DocsIndexUri, "toc.html");
            var tocHtml = await TryDownloadStringAsync(tocUri, cancellationToken);
            toc = tocHtml is not null
                ? ParseDocsToc(tocHtml, tocUri)
                : ParseDocsToc(html, DocsIndexUri);
        }
        else
        {
            // Match Docs behavior: DocFX renders the API navigation from /api/toc.html.
            var tocUri = new Uri(ApiIndexUri, "toc.html");
            var tocHtml = await TryDownloadStringAsync(tocUri, cancellationToken);
            toc = tocHtml is not null
                ? ParseDocsToc(tocHtml, tocUri)
                : ParseApiToc(html);
        }

        SaveTocToDisk(tocPath, toc);
        return toc;
    }

    public async Task<string> GetPageMarkdownAsync(DocsSiteMode mode, Uri url, CancellationToken cancellationToken)
    {
        var pagesDir = Path.Combine(_cacheRoot, mode == DocsSiteMode.Docs ? "pages-docs" : "pages-api");
        Directory.CreateDirectory(pagesDir);

        var key = Sha256(url.ToString());
        var mdPath = Path.Combine(pagesDir, key + ".md");
        var htmlPath = Path.Combine(pagesDir, key + ".html");

        // Use cache first if offline.
        var html = await TryDownloadStringAsync(url, cancellationToken);
        if (html is null)
        {
            if (File.Exists(mdPath))
                return await File.ReadAllTextAsync(mdPath, cancellationToken);

            if (File.Exists(htmlPath))
            {
                var cachedHtml = await File.ReadAllTextAsync(htmlPath, cancellationToken);
                var cachedMd = HtmlToMarkdown(cachedHtml, url);
                await File.WriteAllTextAsync(mdPath, cachedMd, cancellationToken);
                return cachedMd;
            }

            return string.Empty;
        }

        // Persist fresh copy.
        await File.WriteAllTextAsync(htmlPath, html, cancellationToken);

        var markdown = HtmlToMarkdown(html, url);
        await File.WriteAllTextAsync(mdPath, markdown, cancellationToken);
        return markdown;
    }

    private async Task<string?> TryDownloadStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            return await _http.GetStringAsync(uri, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<DocsTocNode> LoadTocFromDisk(string tocPath)
    {
        try
        {
            var json = File.ReadAllText(tocPath);
            var dto = JsonSerializer.Deserialize<List<DocsTocNodeDto>>(json, JsonOptions) ?? new();
            return dto.Select(FromDto).ToList();
        }
        catch
        {
            return Array.Empty<DocsTocNode>();
        }
    }

    private static void SaveTocToDisk(string tocPath, IReadOnlyList<DocsTocNode> toc)
    {
        try
        {
            var dto = toc.Select(ToDto).ToList();
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(tocPath, json);
        }
        catch
        {
            // Best effort.
        }
    }

    private static DocsTocNodeDto ToDto(DocsTocNode node)
        => new(node.Title, node.Url?.ToString(), node.FullTitle, node.Children.Select(ToDto).ToList());

    private static DocsTocNode FromDto(DocsTocNodeDto dto)
    {
        Uri? url = null;
        if (!string.IsNullOrWhiteSpace(dto.Url) && Uri.TryCreate(dto.Url, UriKind.Absolute, out var parsed))
            url = parsed;

        var node = new DocsTocNode(dto.Title, url)
        {
            FullTitle = string.IsNullOrWhiteSpace(dto.FullTitle) ? dto.Title : dto.FullTitle
        };

        if (dto.Children is not null)
        {
            foreach (var child in dto.Children)
                node.Children.Add(FromDto(child));
        }

        return node;
    }

    private static IReadOnlyList<DocsTocNode> ParseDocsToc(string html, Uri baseUri)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // DocFX TOC structure (examples from /articles/toc.html):
        // <div class="sidetoc"><div class="toc" id="toc"><ul class="nav level1"> ...
        var ul = doc.DocumentNode.SelectSingleNode("//*[@id='toc']//ul[contains(@class,'level1')]")
                 ?? doc.DocumentNode.SelectSingleNode("//*[@id='toc']//ul[contains(@class,'nav')]")
                 ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'sidetoc')]//*[@id='toc']//ul")
                 ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'sidetoc')]//ul[contains(@class,'nav')]")
                 ?? doc.DocumentNode.SelectSingleNode("//ul[contains(@class,'nav')]");

        return ul is null
            ? Array.Empty<DocsTocNode>()
            : ParseNestedList(ul, baseUri);
    }

    private static IReadOnlyList<DocsTocNode> ParseApiToc(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Collect links that look like namespace pages.
        var links = doc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(a => new
            {
                Text = HtmlEntity.DeEntitize((a.InnerText ?? string.Empty).Trim()),
                Href = (a.GetAttributeValue("href", string.Empty) ?? string.Empty).Trim()
            })
            .Where(x => x.Href.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && !x.Href.Contains("#"))
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .ToList() ?? new();

        // Normalize to absolute urls and build a tree based on dot-separated segments.
        var roots = new Dictionary<string, DocsTocNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in links)
        {
            if (!Uri.TryCreate(ApiIndexUri, link.Href, out var absolute))
                continue;

            var display = NormalizeDottedName(link.Text);
            if (display.Length == 0)
                continue;

            var segments = display.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddBySegments(roots, segments, absolute, display);
        }

        return roots.Values
            .OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeDottedName(string text)
    {
        // The API site uses "Microsoft. Xna. Framework" formatting.
        var t = text.Replace(" ", string.Empty);
        // Re-insert dots based on original, but simplest is: collapse repeated dots.
        t = t.Replace("..", ".");
        return t.Trim('.');
    }

    private static void AddBySegments(Dictionary<string, DocsTocNode> roots, string[] segments, Uri url, string fullTitle)
    {
        if (segments.Length == 0)
            return;

        var key = segments[0];
        if (!roots.TryGetValue(key, out var node))
        {
            node = new DocsTocNode(segments[0], null);
            roots[key] = node;
        }

        var current = node;
        for (var i = 1; i < segments.Length; i++)
        {
            var seg = segments[i];
            var existing = current.Children.FirstOrDefault(c => string.Equals(c.Title, seg, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new DocsTocNode(seg, null);
                current.Children.Add(existing);
            }

            current = existing;
        }

        // Leaf: set url on the last segment node.
        current.Url = url;
        current.FullTitle = fullTitle;
    }

    private static IReadOnlyList<DocsTocNode> ParseNestedList(HtmlNode ul, Uri baseUri)
    {
        var results = new List<DocsTocNode>();
        var liNodes = ul.SelectNodes("./li") ?? new HtmlNodeCollection(ul);

        foreach (var li in liNodes)
        {
            var a = li.SelectSingleNode("./a[@href]") ?? li.SelectSingleNode(".//a[@href]");

            // DocFX TOCs commonly provide a cleaned title attribute (and InnerText can be truncated/whitespacey).
            var titleAttr = a?.GetAttributeValue("title", null);
            var text = HtmlEntity.DeEntitize(((titleAttr ?? a?.InnerText) ?? li.InnerText ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(text))
                continue;

            Uri? url = null;
            var href = a?.GetAttributeValue("href", string.Empty) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(href) && Uri.TryCreate(baseUri, href, out var absolute))
                url = absolute;

            var node = new DocsTocNode(text, url);

            var childUl = li.SelectSingleNode("./ul") ?? li.SelectSingleNode(".//ul");
            if (childUl is not null)
            {
                var children = ParseNestedList(childUl, baseUri);
                foreach (var child in children)
                    node.Children.Add(child);
            }

            results.Add(node);
        }

        return results;
    }

    private static string HtmlToMarkdown(string html, Uri pageUri)
    {
        // Minimal readable conversion; keep this conservative.
        // DocFX sites (like docs.monogame.net) render the primary content inside:
        //   <div class="content"> ... <article ...> ...
        // Prefer that to keep Docs and API rendering consistent.
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var main = doc.DocumentNode.SelectSingleNode("//*[contains(concat(' ', normalize-space(@class), ' '), ' content ')]//article")
                   ?? doc.DocumentNode.SelectSingleNode("//main//article")
                   ?? doc.DocumentNode.SelectSingleNode("//article")
                   ?? doc.DocumentNode.SelectSingleNode("//main")
                   ?? doc.DocumentNode.SelectSingleNode("//body")
                   ?? doc.DocumentNode;

        var sb = new StringBuilder();
        AppendMarkdown(sb, main, pageUri);

        var md = sb.ToString().Replace("\r\n", "\n");
        return md.Trim();
    }

    private static void AppendMarkdown(StringBuilder sb, HtmlNode node, Uri pageUri)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(child.InnerText);
                text = NormalizeWhitespace(text);
                if (text.Length > 0)
                    sb.Append(text);
                continue;
            }

            if (child.NodeType != HtmlNodeType.Element)
                continue;

            var name = child.Name.ToLowerInvariant();
            switch (name)
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                {
                    var level = name[1] - '0';
                    sb.AppendLine();
                    sb.AppendLine(new string('#', level) + " " + NormalizeWhitespace(HtmlEntity.DeEntitize(child.InnerText)));
                    sb.AppendLine();
                    break;
                }
                case "br":
                {
                    sb.AppendLine();
                    break;
                }
                case "p":
                {
                    sb.AppendLine();
                    // Render paragraph content (preserve inline links/emphasis/code) instead of flattening InnerText.
                    var inner = new StringBuilder();
                    AppendMarkdown(inner, child, pageUri);
                    var text = NormalizeWhitespace(inner.ToString());
                    if (text.Length > 0)
                        sb.AppendLine(text);
                    sb.AppendLine();
                    break;
                }
                case "table":
                {
                    if (TryAppendHtmlTableAsMarkdown(sb, child, pageUri))
                        break;

                    AppendMarkdown(sb, child, pageUri);
                    break;
                }
                case "code":
                {
                    // Inline code (block code handled by <pre>).
                    if (!string.Equals(child.ParentNode?.Name, "pre", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = NormalizeWhitespace(HtmlEntity.DeEntitize(child.InnerText));
                        if (text.Length > 0)
                            sb.Append('`').Append(text).Append('`');
                    }
                    else
                    {
                        AppendMarkdown(sb, child, pageUri);
                    }

                    break;
                }
                case "strong":
                case "b":
                {
                    var text = NormalizeWhitespace(HtmlEntity.DeEntitize(child.InnerText));
                    if (text.Length > 0)
                        sb.Append("**").Append(text).Append("**");
                    break;
                }
                case "em":
                case "i":
                {
                    var text = NormalizeWhitespace(HtmlEntity.DeEntitize(child.InnerText));
                    if (text.Length > 0)
                        sb.Append('*').Append(text).Append('*');
                    break;
                }
                case "pre":
                {
                    var codeNode = child.SelectSingleNode("./code") ?? child.SelectSingleNode(".//code");
                    var code = (codeNode?.InnerText ?? child.InnerText ?? string.Empty);
                    var lang = codeNode?.GetAttributeValue("class", null);
                    // DocFX uses classes like "lang-cs".
                    lang = lang is null ? null : lang.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(c => c.StartsWith("lang-", StringComparison.OrdinalIgnoreCase));

                    var fence = "```";
                    if (!string.IsNullOrWhiteSpace(lang) && lang.Length > 5)
                        fence = "```" + lang[5..];

                    sb.AppendLine();
                    sb.AppendLine(fence);
                    sb.AppendLine(code.Trim('\n', '\r'));
                    sb.AppendLine("```");
                    sb.AppendLine();
                    break;
                }
                case "ul":
                {
                    var lis = child.SelectNodes("./li") ?? new HtmlNodeCollection(child);
                    foreach (var li in lis)
                    {
                        var itemSb = new StringBuilder();
                        AppendMarkdown(itemSb, li, pageUri);

                        // Flatten blocky output for list items while keeping Markdown links intact.
                        var item = NormalizeWhitespace(itemSb.ToString());
                        if (item.Length == 0)
                            continue;

                        sb.AppendLine("- " + item);
                    }

                    sb.AppendLine();
                    break;
                }
                case "ol":
                {
                    var lis = child.SelectNodes("./li") ?? new HtmlNodeCollection(child);
                    var number = 1;

                    foreach (var li in lis)
                    {
                        var itemSb = new StringBuilder();
                        AppendMarkdown(itemSb, li, pageUri);

                        var item = NormalizeWhitespace(itemSb.ToString());
                        if (item.Length == 0)
                            continue;

                        sb.AppendLine($"{number}. {item}");
                        number++;
                    }

                    sb.AppendLine();
                    break;
                }
                case "a":
                {
                    var text = NormalizeWhitespace(HtmlEntity.DeEntitize(child.InnerText));
                    var href = (child.GetAttributeValue("href", string.Empty) ?? string.Empty).Trim();
                    if (Uri.TryCreate(pageUri, href, out var absolute) && text.Length > 0)
                        sb.Append($"[{text}]({absolute})");
                    else
                        sb.Append(text);
                    break;
                }
                case "script":
                case "style":
                case "nav":
                case "footer":
                case "header":
                case "form":
                case "button":
                {
                    break;
                }
                case "div":
                {
                    // DocFX pages on docs.monogame.net sometimes use a grid of "mg-card" tiles inside a bootstrap row.
                    // Convert those to a clean ordered list (title + description) instead of a blob of links.
                    if (HasClass(child, "row") && TryAppendMgCardRowAsOrderedList(sb, child, pageUri))
                        break;

                    // Drop non-content DocFX blocks that repeat across pages.
                    var cls = child.GetAttributeValue("class", string.Empty) ?? string.Empty;
                    if (cls.Contains("contribution", StringComparison.OrdinalIgnoreCase) ||
                        cls.Contains("next-article", StringComparison.OrdinalIgnoreCase) ||
                        cls.Contains("actionbar", StringComparison.OrdinalIgnoreCase) ||
                        cls.Contains("toc-offcanvas", StringComparison.OrdinalIgnoreCase) ||
                        cls.Contains("search-results", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    AppendMarkdown(sb, child, pageUri);
                    break;
                }
                default:
                {
                    AppendMarkdown(sb, child, pageUri);
                    break;
                }
            }
        }
    }

    private static bool TryAppendHtmlTableAsMarkdown(StringBuilder sb, HtmlNode table, Uri pageUri)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows is null || rows.Count == 0)
            return false;

        // Prefer explicit header rows in <thead>, otherwise use the first row as the header.
        var headRow = table.SelectSingleNode("./thead//tr")
            ?? table.SelectSingleNode(".//thead//tr")
            ?? rows[0];

        static List<HtmlNode> GetCells(HtmlNode row)
        {
            var cells = row.SelectNodes("./th|./td");
            return cells is null ? new List<HtmlNode>() : cells.ToList();
        }

        string RenderCell(HtmlNode cell)
        {
            var cellSb = new StringBuilder();
            AppendMarkdown(cellSb, cell, pageUri);

            var text = NormalizeWhitespace(cellSb.ToString());
            if (text.Length == 0)
                text = " ";

            // Markdown table cells treat '|' as a delimiter.
            text = text.Replace("|", "\\|");
            return text;
        }

        var headerCellsNodes = GetCells(headRow);
        if (headerCellsNodes.Count == 0)
            return false;

        var headerCells = headerCellsNodes.Select(RenderCell).ToList();

        // Determine max column count across all rows.
        var columnCount = headerCells.Count;
        foreach (var row in rows)
        {
            var count = GetCells(row).Count;
            if (count > columnCount)
                columnCount = count;
        }

        if (columnCount <= 0)
            return false;

        void PadTo(List<string> list)
        {
            while (list.Count < columnCount)
                list.Add(" ");
        }

        PadTo(headerCells);

        // Write table.
        sb.AppendLine();
        sb.AppendLine("| " + string.Join(" | ", headerCells) + " |");
        sb.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", columnCount)) + " |");

        foreach (var row in rows)
        {
            // Skip the header row if it came from the first <tr> (common case).
            if (ReferenceEquals(row, headRow))
                continue;

            var rowCells = GetCells(row).Select(RenderCell).ToList();
            if (rowCells.Count == 0)
                continue;

            PadTo(rowCells);
            sb.AppendLine("| " + string.Join(" | ", rowCells) + " |");
        }

        sb.AppendLine();
        return true;
    }

    private static bool TryAppendMgCardRowAsOrderedList(StringBuilder sb, HtmlNode rowDiv, Uri pageUri)
    {
        // Expect structure like:
        // <div class="row"> <div class="col-.."> <div class="mg-card ..."> <a href="..."> ... </a> </div>
        var cardNodes = rowDiv.SelectNodes(".//*[contains(concat(' ', normalize-space(@class), ' '), ' mg-card ')][.//a[@href]]");
        if (cardNodes is null || cardNodes.Count == 0)
            return false;

        // Filter to actual card roots (class token == "mg-card" and not nested under another mg-card).
        var cards = new List<HtmlNode>();
        foreach (var n in cardNodes)
        {
            if (!HasClass(n, "mg-card"))
                continue;

            var parent = n.ParentNode;
            var nested = false;
            while (parent is not null)
            {
                if (HasClass(parent, "mg-card"))
                {
                    nested = true;
                    break;
                }

                parent = parent.ParentNode;
            }

            if (!nested)
                cards.Add(n);
        }

        if (cards.Count == 0)
            return false;

        sb.AppendLine();
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var a = card.SelectSingleNode(".//a[@href]");
            if (a is null)
                continue;

            var href = (a.GetAttributeValue("href", string.Empty) ?? string.Empty).Trim();
            Uri? url = null;
            if (!string.IsNullOrWhiteSpace(href) && Uri.TryCreate(pageUri, href, out var absolute))
                url = absolute;

            var titleNode = card.SelectSingleNode(".//*[contains(concat(' ', normalize-space(@class), ' '), ' mg-card-title ')]");
            var title = NormalizeWhitespace(HtmlEntity.DeEntitize((titleNode?.InnerText ?? a.GetAttributeValue("title", string.Empty) ?? a.InnerText ?? string.Empty)));
            if (title.Length == 0)
                title = "Link";

            // Prefer the first descriptive paragraph; ignore the CTA (usually p.mt-auto).
            var pNodes = card.SelectNodes(".//p") ?? new HtmlNodeCollection(card);
            string description = string.Empty;
            foreach (var p in pNodes)
            {
                var pClass = p.GetAttributeValue("class", string.Empty) ?? string.Empty;
                if (pClass.Contains("mt-auto", StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = NormalizeWhitespace(HtmlEntity.DeEntitize(p.InnerText));
                if (text.Length == 0)
                    continue;

                description = text;
                break;
            }

            var number = i + 1;
            if (url is not null)
                sb.Append($"{number}. [{title}]({url})");
            else
                sb.Append($"{number}. {title}");

            if (!string.IsNullOrWhiteSpace(description))
                sb.Append(" — ").Append(description);

            sb.AppendLine();
        }

        sb.AppendLine();
        return true;
    }

    private static bool HasClass(HtmlNode node, string className)
    {
        var cls = node.GetAttributeValue("class", string.Empty) ?? string.Empty;
        if (cls.Length == 0)
            return false;

        foreach (var token in cls.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(token, className, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;

        foreach (var ch in text)
        {
            var isSpace = char.IsWhiteSpace(ch);
            if (isSpace)
            {
                if (!lastWasSpace)
                    sb.Append(' ');
                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;
            sb.Append(ch);
        }

        return sb.ToString().Trim();
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private sealed record DocsTocNodeDto(string Title, string? Url, string? FullTitle, List<DocsTocNodeDto>? Children);
}
