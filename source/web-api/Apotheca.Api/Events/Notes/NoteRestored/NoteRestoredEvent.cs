namespace Apotheca.Api.Events.Notes.NoteRestored;

public class NoteRestoredEvent
{
    public const string TopicId = "note-restored";

    public string NoteId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public IReadOnlyList<RestoredAncestor> RestoredAncestors { get; init; } = [];
}

public class RestoredAncestor
{
    public string NoteId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
}
