namespace Apotheca.Api.Events.Documents.DocumentRestored;

public class DocumentRestoredEvent
{
    public const string TopicId = "document-restored";

    public string DocumentId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public IReadOnlyList<RestoredAncestor> RestoredAncestors { get; init; } = [];
}

public class RestoredAncestor
{
    public string DocumentId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
}
