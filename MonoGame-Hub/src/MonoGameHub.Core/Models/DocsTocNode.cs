namespace MonoGameHub.Core.Models;

public sealed class DocsTocNode
{
    public DocsTocNode(string title, Uri? url)
    {
        Title = title;
        Url = url;
        FullTitle = title;
    }

    public string Title { get; }

    // Null for non-leaf nodes.
    public Uri? Url { get; set; }

    // Used for API where leaf title may be a full dotted name.
    public string FullTitle { get; set; }

    public List<DocsTocNode> Children { get; } = new();
}
