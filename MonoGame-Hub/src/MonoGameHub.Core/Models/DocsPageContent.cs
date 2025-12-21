namespace MonoGameHub.Core.Models;

public abstract record DocsPageBlock;

public abstract record DocsInline;

public sealed record DocsTextInline(string Text) : DocsInline;

public sealed record DocsLinkInline(string Text, string Url) : DocsInline;

public sealed record DocsHeadingBlock(int Level, string Text) : DocsPageBlock;

public sealed record DocsParagraphBlock(IReadOnlyList<DocsInline> Inlines) : DocsPageBlock;

public sealed record DocsListBlock(bool Ordered, IReadOnlyList<IReadOnlyList<DocsInline>> Items) : DocsPageBlock;

public sealed record DocsCodeBlock(string Code, string? Language) : DocsPageBlock;

public sealed record DocsPageTableBlock(DocsTable Table) : DocsPageBlock;

public sealed record DocsPageContent(IReadOnlyList<DocsPageBlock> Blocks);

public sealed record DocsTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows)
{
    public int ColumnCount
    {
        get
        {
            var max = Headers.Count;
            foreach (var row in Rows)
                max = Math.Max(max, row.Count);
            return max;
        }
    }
}
